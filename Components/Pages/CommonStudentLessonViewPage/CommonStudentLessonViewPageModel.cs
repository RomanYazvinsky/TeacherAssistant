using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Containers.Annotations;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class CommonStudentLessonViewPageModel : AbstractModel {
        private readonly ObservableRangeCollection<StudentLessonView> _items =
            new WpfObservableRangeCollection<StudentLessonView>();

        public CommonStudentLessonViewPageModel(string id) : base(id) {
            this.Items = new CollectionViewSource {
                Source = _items
            };
            this.Columns.Add(BuildMissedLessonsColumn());
            this.WhenAnyValue(model => model.Columns).Subscribe();
            this.WhenAnyValue(model => model.FilterText)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => {
                    if (string.IsNullOrWhiteSpace(this.FilterText)) {
                        UpdateFromAsync(() => this.Items.View.Filter = __ => true);
                        return;
                    }

                    UpdateFromAsync(() => this.Items.View.Filter =
                        o => o is StudentLessonView item &&
                             item.FullName.ToUpper().Contains(this.FilterText.ToUpper()));
                });
        }

        private LessonModel _currentLesson;

        private Dictionary<string, LessonModel> LessonModels { get; set; } = new Dictionary<string, LessonModel>();

        public ObservableRangeCollection<StudentLessonModel> StudentLessons =
            new WpfObservableRangeCollection<StudentLessonModel>();

        [Reactive] public string FilterText { get; set; }

        public CollectionViewSource Items { get; set; }

        public ObservableRangeCollection<DataGridColumn> Columns { get; set; } =
            new WpfObservableRangeCollection<DataGridColumn>();

        private DataGridColumn BuildMissedLessonsColumn() {
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(80),
                    Header = new TextBlock {Text = Localization["Пропуски"]},
                    Binding = new Binding("MissedLessons"),
                    IsReadOnly = true
                };
            return dataGridTemplateColumn;
        }

        private DataGridColumn BuildStudentLessonColumn(string id, LessonModel model) {
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(90),
                    MinWidth = 50,
                    Header = new TextBlock {
                        Text = Localization["common.lesson.type." + model.LessonType] + "\n " +
                               model.Date?.ToString("dd.MM"),
                        Foreground = model.Id == _currentLesson.Id ? Brushes.Red : Brushes.Black
                    },
                    Binding = new Binding {Path = new PropertyPath($"LessonToLessonMark[{id}].Mark")}
                };
            var cellStyle = new Style();
            var setterBase = new Setter(Control.BackgroundProperty, new Binding($"LessonToLessonMark[{id}].Color"));
            cellStyle.Setters.Add(setterBase);
            dataGridTemplateColumn.CellStyle = cellStyle;
            return dataGridTemplateColumn;
        }

        protected override string GetLocalizationKey() {
            return "common.lesson.view";
        }

        public override Task Init() {
            Select<LessonModel>(this.Id, "Lesson").Subscribe(async lesson => {
                if (lesson == null) {
                    return;
                }

                _items.Clear();
                this.Columns.Clear();
                _currentLesson = lesson;
                List<LessonModel> lessonModels;
                lessonModels = lesson.Group == null
                    ? await _db.LessonModels.Include("StudentLessons").Where(model =>
                            model._StreamId == lesson._StreamId && (model._GroupId == null || model._GroupId == 0))
                        .ToListAsync()
                    : await _db.LessonModels
                        .Include("StudentLessons")
                        .Where(model => model._GroupId == lesson._GroupId
                                        || (model._StreamId == lesson._StreamId
                                            && !model._GroupId.HasValue))
                        .ToListAsync();
                lessonModels.Reverse();
                foreach (var lessonModel in lessonModels) {
                    var id = IdGenerator.GenerateId(10);
                    this.LessonModels.Add(id, lessonModel);
                    this.Columns.Add(BuildStudentLessonColumn(id, lessonModel));
                }

                var studentModels = (lesson.Group == null
                        ? lesson.Stream.Groups.Aggregate(new List<StudentModel>(),
                            (list, model) => {
                                list.AddRange(model.Students);
                                return list;
                            })
                        : lesson.Group.Students.ToList())
                    .Select(model => new StudentLessonView(this.Id, model, this.LessonModels))
                    .OrderBy(view => view.FullName).ToList();
                _items.AddRange(studentModels);
            });
            return Task.CompletedTask;
        }
    }

    public class StudentLessonView : INotifyPropertyChanged {
        private int _missedLessons = 0;

        private Dictionary<string, StudentLessonMarkModel> _lessonToLessonMark =
            new Dictionary<string, StudentLessonMarkModel>();

        public string TableId { get; }

        public string FullName { get; set; }

        public int MissedLessons {
            get => _missedLessons;
            set {
                if (value == _missedLessons) return;
                _missedLessons = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, StudentLessonMarkModel> LessonToLessonMark {
            get => _lessonToLessonMark;
            set {
                if (Equals(value, _lessonToLessonMark)) return;
                _lessonToLessonMark = value;
                OnPropertyChanged();
            }
        }

        public StudentModel Model { get; set; }

        public StudentLessonView(string tableId, StudentModel student, Dictionary<string, LessonModel> lessonModels) {
            this.TableId = tableId;
            this.Model = student;
            this.FullName = student.LastName + " " + student.FirstName;
            foreach (var keyValuePair in lessonModels) {
                var studentLessonModel =
                    keyValuePair.Value.StudentLessons.FirstOrDefault(model => model._StudentId == student.Id) ??
                    new StudentLessonModel {
                        Lesson = keyValuePair.Value,
                        Student = student,
                        IsRegistered = false,
                        Mark = ""
                    };
                if (studentLessonModel.Id == 0) {
                    GeneralDbContext.Instance.StudentLessonModels.Add(studentLessonModel);
                    GeneralDbContext.Instance.ThrottleSave();
                }

                this.LessonToLessonMark.Add(keyValuePair.Key, new StudentLessonMarkModel(studentLessonModel));
            }

            this.MissedLessons =
                this.LessonToLessonMark.Values.Aggregate(0, (i, model) => model.Model.IsLessonMissed ? i + 1 : i);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StudentLessonMarkModel : INotifyPropertyChanged {
        public StudentLessonModel Model { get; set; }

        public StudentLessonMarkModel(StudentLessonModel model) {
            this.Model = model;
            this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
        }

        public string Mark {
            get => Model.Mark;
            set {
                if (Model.Mark.Equals(value)) return;
                Model.Mark = value;
                GeneralDbContext.Instance.ThrottleSave();
                OnPropertyChanged();
            }
        }

        public Brush Color { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
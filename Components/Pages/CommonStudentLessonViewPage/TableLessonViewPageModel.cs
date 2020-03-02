using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Kernel;
using Grace.DependencyInjection;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewPageModel : AbstractModel<TableLessonViewPageModel> {
        private const int AttestationCountToCalculateAvg = 2;
        private readonly LocalDbContext _db;
        private readonly IExportLocatorScope _scope;
        private readonly TabPageHost _host;
        private readonly Style _studentLessonMarkCellStyle;
        private readonly Style _columnHeaderStyle;

        [NotNull] private static readonly ResourceDictionary DataGridResources
            = new ResourceDictionary {
                Source = new Uri(
                    "/TeacherAssistant.Components;component/Pages/CommonStudentLessonViewPage/Styles/Styles.xaml",
                    UriKind.RelativeOrAbsolute)
            };

        private readonly ObservableCollection<StudentLessonViewModel> _items =
            new ObservableCollection<StudentLessonViewModel>();

        public TableLessonViewPageModel(
            TableLessonViewToken token, LocalDbContext db, IExportLocatorScope scope, TabPageHost host
        ) {
            _db = db;
            _scope = scope;
            _host = host;
            this.Items = new CollectionViewSource {
                Source = _items
            };
            _studentLessonMarkCellStyle = DataGridResources["StudentLessonMarkCell"] as Style;
            _columnHeaderStyle = DataGridResources["ColumnHeaderStyle"] as Style;
            this.WhenActivated(c => {
                this.WhenAnyValue(model => model.FilterText)
                    .Throttle(TimeSpan.FromMilliseconds(300))
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(_ => {
                        if (string.IsNullOrWhiteSpace(this.FilterText)) {
                            this.Items.View.Filter = null;
                            return;
                        }

                        this.Items.View.Filter =
                            o => o is StudentLessonViewModel item &&
                                 item.FullName.ToUpper().Contains(this.FilterText.ToUpper());
                    }).DisposeWith(c);
            });

            Init(token.Lesson);
        }

        private async Task Init(LessonEntity lesson) {
            _items.Clear();
            this.Columns.Clear();
            this.Columns.Add(BuildMissedLessonsColumn());
            _currentLesson = lesson;
            List<LessonEntity> lessonModels;
            if (lesson.Group == null) {
                lessonModels = await GetStreamLessonsAsync(lesson);
            }
            else {
                lessonModels = await GetGroupLessonsAsync(lesson);
            }

            lessonModels.Sort(SortLessons);
            if (lessonModels.Count(ls => ls.LessonType == LessonType.Attestation) >= AttestationCountToCalculateAvg) {
                this.Columns.Add(BuildAttestationSummaryColumn());
            }

            foreach (var lessonModel in lessonModels) {
                var lessonId = IdGenerator.GenerateId();
                this._lessons.Add(lessonId, lessonModel);
                this.Columns.Add(BuildStudentLessonColumn(lessonId, lessonModel));
            }

            var studentModels = ((lesson.Group == null
                    ? lesson.Stream.Groups?.SelectMany(group =>
                        group.Students ?? new List<StudentEntity>())
                    : lesson.Group.Students?.ToList()) ?? new List<StudentEntity>())
                .Select(model => new StudentLessonViewModel(model, this._lessons, _scope, _host, _db))
                .OrderBy(view => view.FullName).ToList();
            _items.AddRange(studentModels);
        }

        private LessonEntity _currentLesson;

        private Task<List<LessonEntity>> GetStreamLessonsAsync(LessonEntity lesson) {
            return _db.Lessons
                .Include(ls => ls.StudentLessons)
                .Where(model =>
                    model._StreamId == lesson._StreamId && (model._GroupId == null || model._GroupId == 0))
                .ToListAsync();
        }

        private Task<List<LessonEntity>> GetGroupLessonsAsync(LessonEntity lesson) {
            return _db.Lessons
                .Include(ls => ls.StudentLessons)
                .Where(model => model._GroupId == lesson._GroupId
                                || (model._StreamId == lesson._StreamId
                                    && !model._GroupId.HasValue))
                .ToListAsync();
        }

        private readonly Dictionary<string, LessonEntity> _lessons = new Dictionary<string, LessonEntity>();

        [Reactive] public string FilterText { get; set; }

        public CollectionViewSource Items { get; }

        public ObservableCollection<DataGridColumn> Columns { get; set; } =
            new ObservableCollection<DataGridColumn>();

        private DataGridColumn BuildMissedLessonsColumn() {
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                    Header = new TextBlock {Text = Localization["Пропуски"]},
                    Binding = new Binding("MissedLessons"),
                    IsReadOnly = true,
                    CanUserSort = false
                };
            return dataGridTemplateColumn;
        }

        private DataGridColumn BuildAttestationSummaryColumn() {
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                    Header = new TextBlock {
                        Text = Localization["Аттестация\n(общ)"]
                    },
                    Binding = new Binding("AttestationSummary"),
                    IsReadOnly = true,
                    CanUserSort = false
                };
            return dataGridTemplateColumn;
        }

        private SolidColorBrush GetLessonColumnHeaderBackgroundColor([NotNull] LessonEntity lesson) {
            switch (lesson.LessonType) {
                case LessonType.Exam:
                    return new SolidColorBrush(Color.FromArgb(20, 0, 200, 20));
                case LessonType.Attestation:
                    return new SolidColorBrush(Color.FromArgb(20, 200, 200, 20));
                case LessonType.Laboratory:
                    return new SolidColorBrush(Color.FromArgb(20, 200, 20, 150));
                case LessonType.Practice:
                    return new SolidColorBrush(Color.FromArgb(20, 0, 20, 200));
                default:
                    return Brushes.Transparent;
            }
        }

        private DataGridColumn BuildStudentLessonColumn([NotNull] string id, [NotNull] LessonEntity entity) {
            var header = new Grid {
                Background = GetLessonColumnHeaderBackgroundColor(entity),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            header.Children.Add(new TextBlock {
                Text = Localization["common.lesson.type." + entity.LessonType] + "\n " +
                       entity.Date?.ToString("dd.MM"),
                Foreground = entity.Id == _currentLesson.Id ? Brushes.Red : Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(90),
                    MinWidth = 50,
                    Header = header,
                    Binding = new Binding {Path = new PropertyPath($"LessonToLessonMark[{id}].Mark")},
                    CanUserSort = false
                };
            var cellStyle = new Style {
                BasedOn = _studentLessonMarkCellStyle,
                TargetType = typeof(DataGridCell)
            };
            var backGround = new Setter(Control.BackgroundProperty, new Binding($"LessonToLessonMark[{id}].Color"));
            var menu = new Setter(FrameworkElement.ContextMenuProperty, BuildLessonCellContextMenu(id));
            cellStyle.Setters.Add(backGround);
            cellStyle.Setters.Add(menu);
            dataGridTemplateColumn.CellStyle = cellStyle;
            dataGridTemplateColumn.HeaderStyle = _columnHeaderStyle;
            return dataGridTemplateColumn;
        }

        private ContextMenu BuildLessonCellContextMenu(string id) {
            var menu = new ContextMenu();
            var toggleItem = new MenuItem();
            toggleItem.SetBinding(MenuItem.CommandProperty,
                new Binding($"LessonToLessonMark[{id}].ToggleRegistrationHandler"));
            toggleItem.Header = "Отметить/пропуск";
            var openItem = new MenuItem();
            openItem.SetBinding(MenuItem.CommandProperty,
                new Binding($"LessonToLessonMark[{id}].OpenRegistrationHandler"));
            openItem.Header = "Регистрация";
            menu.Items.Add(toggleItem);
            menu.Items.Add(openItem);
            return menu;
        }

        private int SortLessons(LessonEntity lesson1, LessonEntity lesson2) {
            if (lesson1.LessonType == LessonType.Attestation
                && lesson2.LessonType == LessonType.Attestation) {
                return lesson2.Date?.CompareTo(lesson1.Date.ValueOr(DateTime.MinValue)) ?? -1;
            }

            if (lesson1.LessonType == LessonType.Attestation
                && lesson2.LessonType == LessonType.Exam) {
                return 1;
            }

            if (lesson1.LessonType == LessonType.Exam
                && lesson2.LessonType == LessonType.Attestation) {
                return -1;
            }

            if (lesson1.LessonType == LessonType.Attestation) {
                return -1;
            }

            if (lesson1.LessonType == LessonType.Exam) {
                return -1;
            }

            if (lesson2.LessonType == LessonType.Attestation) {
                return 1;
            }

            if (lesson2.LessonType == LessonType.Exam) {
                return 1;
            }

            return lesson2.Date?.CompareTo(lesson1.Date.ValueOr(DateTime.MinValue)) ?? -1;
        }

        protected override string GetLocalizationKey() {
            return "common.lesson.view";
        }
    }
}
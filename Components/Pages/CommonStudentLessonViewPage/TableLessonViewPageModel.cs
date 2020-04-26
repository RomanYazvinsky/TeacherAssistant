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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Containers;
using DynamicData;
using DynamicData.Kernel;
using FontAwesome5;
using Grace.DependencyInjection;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Utils;
using TeacherAssistant.Pages.PageController;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewPageModel : AbstractModel<TableLessonViewPageModel> {
        private const int AttestationCountToCalculateAvg = 2;
        private readonly WindowComponentHost _windowComponentHost;
        private readonly LocalDbContext _db;
        private readonly IExportLocatorScope _scope;
        private readonly TabComponentHost _host;
        private bool _shortMode = true;

        private readonly ObservableCollection<StudentRowViewModel> _items =
            new ObservableCollection<StudentRowViewModel>();

        private readonly Dictionary<long, LessonEntity> _lessons = new Dictionary<long, LessonEntity>();

        private readonly DataGridLessonColumnBuilder _columnBuilder;

        public TableLessonViewPageModel(
            ModuleActivation<TableLessonViewToken> activation,
            WindowComponentHost windowComponentHost,
            LocalDbContext db,
            IExportLocatorScope scope,
            TabComponentHost host,
            PageControllerReducer pageControllerReducer
        ) {
            _windowComponentHost = windowComponentHost;
            _db = db;
            _scope = scope;
            _host = host;
            _columnBuilder = new DataGridLessonColumnBuilder(windowComponentHost, activation.Token.Lesson, Localization);
            this.Items = new CollectionViewSource {
                Source = _items
            };
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
                            o => o is StudentRowViewModel item &&
                                 item.FullName.ToUpper().Contains(this.FilterText.ToUpper());
                    }).DisposeWith(c);
            });

            _ = InitAsync(activation.Token.Lesson).ConfigureAwait(false);
            pageControllerReducer.Dispatch(new RegisterControlsAction(activation, GetControls().ToList()));
        }

        private async Task InitAsync(LessonEntity lesson) {
            _lessons.Clear();
            var lessonStudents = await GetLessonStudentsAsync(_db, lesson);
            var lessons = await GetLessonsAsync(lesson);
            var isStudentLessonsAdded = await AddMissedStudentToLessonEntitiesAsync(_db, lessons, lessonStudents);
            if (isStudentLessonsAdded) {
                lessons = await GetLessonsAsync(lesson);
            }

            var showAttestationSummary = lessons.Count(ls => ls.LessonType == LessonType.Attestation) >=
                                         AttestationCountToCalculateAvg;

            lessons.Sort(LessonComparator.Comparator);

            var columnHelper = new StudentLessonColumnHelper();

            foreach (var ls in lessons) {
                _lessons.Add(ls.Id, ls);
            }

            var lessonIds = lessons.Select(ls => ls.Id);
            var studentLessonNotes = await _db.StudentLessonNotes
                .Where(note => lessonIds.Contains(note.StudentLesson._LessonId))
                .ToListAsync();
            var students = lessonStudents
                .Select(model =>
                    new StudentRowViewModel(model, _lessons, _scope, _host, _windowComponentHost, _db, studentLessonNotes))
                .OrderBy(view => view.FullName)
                .ToList();
            await RunInUiThread(() => {
                var columns = new List<DataGridColumn> {_columnBuilder.BuildMissedLessonsColumn()};
                if (showAttestationSummary) {
                    columns.Add(_columnBuilder.BuildAttestationSummaryColumn());
                }

                _items.Clear();
                _items.AddRange(students);
                columns.AddRange(lessons.Select(ls => _columnBuilder.BuildStudentLessonColumn(columnHelper, ls)));
                this.Columns.Clear();
                this.Columns.AddRange(columns);
            });
        }

        private static async Task<bool> AddMissedStudentToLessonEntitiesAsync(
            LocalDbContext db,
            IEnumerable<LessonEntity> lessons,
            IEnumerable<StudentEntity> students
        ) {
            var lessonsArray = lessons as LessonEntity[] ?? lessons.ToArray();
            var studentsArray = students as StudentEntity[] ?? students.ToArray();
            var studentLessonsToAdd = lessonsArray
                .SelectMany(lesson => {
                    var studentToAddToLesson = studentsArray
                        .Where(student => lesson.StudentLessons?.All(stl => stl._StudentId != student.Id) ?? false)
                        .ToArray();
                    if (!studentToAddToLesson.Any()) {
                        return Enumerable.Empty<StudentLessonEntity>();
                    }

                    return studentToAddToLesson.Select(student => new StudentLessonEntity {
                        Lesson = lesson,
                        _LessonId = lesson.Id,
                        Student = student,
                        _StudentId = student.Id,
                        IsRegistered = false
                    });
                })
                .ToArray();
            if (!studentLessonsToAdd.Any()) {
                return false;
            }

            db.StudentLessons.AddRange(studentLessonsToAdd);
            await db.SaveChangesAsync();
            return true;
        }

        private static Task<List<StudentEntity>> GetLessonStudentsAsync(LocalDbContext db, LessonEntity lesson) {
            var lessonStudents = db.GetLessonStudents(lesson)
                .Select(st => st.Id);
            var enumerable = lessonStudents.ToArray();
            return db.Students
                .Include(nameof(StudentEntity.Notes))
                .Where(s => enumerable.Contains(s.Id))
                .ToListAsync();
        }

        private Task<List<LessonEntity>> GetLessonsAsync(LessonEntity lesson) {
            return lesson.Group == null ? GetStreamLessonsAsync(lesson) : GetGroupLessonsAsync(lesson);
        }

        private Task<List<LessonEntity>> GetStreamLessonsAsync(LessonEntity lesson) {
            return _db.Lessons
                .Include(ls => ls.StudentLessons)
                .Include(ls => ls.Notes)
                .Where(model =>
                    model._StreamId == lesson._StreamId && (model._GroupId == null || model._GroupId == 0))
                .ToListAsync();
        }

        private Task<List<LessonEntity>> GetGroupLessonsAsync(LessonEntity lesson) {
            return _db.Lessons
                .Include(ls => ls.StudentLessons)
                .Include(ls => ls.Notes)
                .Where(model => model._GroupId == lesson._GroupId
                                || (model._StreamId == lesson._StreamId
                                    && !model._GroupId.HasValue))
                .ToListAsync();
        }

        private IEnumerable<ButtonConfig> GetControls() {
            return new[] {
                new ButtonConfig {
                    Command = ReactiveCommand.Create(ChangeMode),
                    Text = "Сменить режим"
                }
            };
        }

        private void ChangeMode() {
            _shortMode = !_shortMode;
            if (_shortMode) {
                
            }
        }

        [Reactive] public string FilterText { get; set; }
        [Reactive] public bool Virtualization { get; set; }

        public CollectionViewSource Items { get; }

        public ObservableCollection<DataGridColumn> Columns { get; set; } = new ObservableCollection<DataGridColumn>();


        protected override string GetLocalizationKey() {
            return "common.lesson.view";
        }
    }
}
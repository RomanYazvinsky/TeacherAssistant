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
using DynamicData;
using DynamicData.Kernel;
using FontAwesome5;
using Grace.DependencyInjection;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class TableLessonViewPageModel : AbstractModel<TableLessonViewPageModel> {
        private const int AttestationCountToCalculateAvg = 2;
        private readonly WindowPageHost _windowPageHost;
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

        private readonly ObservableCollection<StudentRowViewModel> _items =
            new ObservableCollection<StudentRowViewModel>();

        public TableLessonViewPageModel(
            TableLessonViewToken token,
            WindowPageHost windowPageHost,
            LocalDbContext db,
            IExportLocatorScope scope,
            TabPageHost host
        ) {
            _windowPageHost = windowPageHost;
            _db = db;
            _scope = scope;
            _host = host;
            this.Items = new CollectionViewSource {
                Source = _items
            };
            _studentLessonMarkCellStyle = DataGridResources["StudentLessonMarkCell"] as Style;
            _columnHeaderStyle = DataGridResources["ColumnHeaderStyle"] as Style;
            RowStyle = DataGridResources["RowStyle"] as Style;
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

            Init(token.Lesson);
        }

        private async Task Init(LessonEntity lesson) {
            _items.Clear();
            this.Columns.Clear();
            this.Columns.Add(BuildMissedLessonsColumn());
            _currentLesson = lesson;
            List<LessonEntity> lessons;
            if (lesson.Group == null) {
                lessons = await GetStreamLessonsAsync(lesson);
            }
            else {
                lessons = await GetGroupLessonsAsync(lesson);
            }

            lessons.Sort(SortLessons);
            if (lessons.Count(ls => ls.LessonType == LessonType.Attestation) >= AttestationCountToCalculateAvg) {
                this.Columns.Add(BuildAttestationSummaryColumn());
            }

            var columnHelper = new StudentLessonColumnHelper();
            foreach (var ls in lessons) {
                this._lessons.Add(ls.Id, ls);
                this.Columns.Add(BuildStudentLessonColumn(columnHelper, ls));
            }

            var studentModels = ((lesson.Group == null
                    ? lesson.Stream.Groups?.SelectMany(group =>
                        group.Students ?? new List<StudentEntity>())
                    : lesson.Group.Students?.ToList()) ?? new List<StudentEntity>())
                .Select(model => new StudentRowViewModel(model, this._lessons, _scope, _host, _db))
                .OrderBy(view => view.FullName).ToList();
            _items.AddRange(studentModels);
        }

        private LessonEntity _currentLesson;

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

        private readonly Dictionary<long, LessonEntity> _lessons = new Dictionary<long, LessonEntity>();

        [Reactive] public string FilterText { get; set; }
        public Style RowStyle { get; set; }

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

        private DataGridColumn BuildStudentLessonColumn(
            [NotNull] StudentLessonColumnHelper helper,
            [NotNull] LessonEntity lesson
        ) {
            var header = new Grid {
                Background = GetLessonColumnHeaderBackgroundColor(lesson),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            header.Children.Add(new TextBlock {
                Text = Localization["common.lesson.type." + lesson.LessonType] + "\n " +
                       lesson.Date?.ToString("dd.MM"),
                Foreground = lesson.Id == _currentLesson.Id ? Brushes.Red : Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            AddLessonNoteInfoToColumnHeader(header, lesson);
            var dataGridTemplateColumn = new StudentLessonColumn(helper, lesson) {
                Width = new DataGridLength(90),
                MinWidth = 50,
                Header = header,
                CanUserSort = false
            };
            var cellStyle = new Style {
                BasedOn = _studentLessonMarkCellStyle,
                TargetType = typeof(DataGridCell)
            };
            dataGridTemplateColumn.CellStyle = cellStyle;
            dataGridTemplateColumn.HeaderStyle = _columnHeaderStyle;
            return dataGridTemplateColumn;
        }

        private void AddLessonNoteInfoToColumnHeader(Panel columnHeader, LessonEntity lesson) {
            var lessonNotes = lesson.Notes?.ToList();
            var headerContextMenu = columnHeader.ContextMenu = new ContextMenu();
            var openNotes = new MenuItem {
                Command = BuildOpenLessonNotesCommand(lesson),
                Header = Localization["Открыть заметки"]
            };
            headerContextMenu.Items.Add(openNotes);
            if (!(lessonNotes?.Any() ?? false)) return;
            var icon = new FontAwesome {
                Icon = EFontAwesomeIcon.Solid_InfoCircle,
                Background = Brushes.Transparent
            };
            columnHeader.Children.Add(icon);
        }

        private ICommand BuildOpenLessonNotesCommand([NotNull] LessonEntity lesson) {
            return ReactiveCommand.Create(() => {
                _windowPageHost.AddPageAsync(new NoteListFormToken("Заметки", () => new LessonNote {
                    Lesson = lesson,
                    EntityId = lesson.Id
                }, lesson.Notes));
            });
        }

        private static int SortLessons(LessonEntity lesson1, LessonEntity lesson2) {
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
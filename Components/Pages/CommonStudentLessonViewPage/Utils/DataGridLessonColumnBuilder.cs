using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome5;
using JetBrains.Annotations;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns;
using TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Utils {
    public class DataGridLessonColumnBuilder {
        private static readonly ResourceDictionary DataGridResources
            = new ResourceDictionary {
                Source = new Uri(
                    "/TeacherAssistant.Components;component/Pages/CommonStudentLessonViewPage/Styles/Styles.xaml",
                    UriKind.RelativeOrAbsolute)
            };

        private static readonly Style StudentLessonMarkCellStyle = DataGridResources["StudentLessonMarkCell"] as Style;
        private static readonly Style ColumnHeaderStyle = DataGridResources["ColumnHeaderStyle"] as Style;
        public static readonly Style RowStyle = DataGridResources["RowStyle"] as Style;

        private readonly WindowComponentHost _windowComponentHost;
        private readonly LessonEntity _currentLesson;
        private readonly LocalizationContainer _localization;

        public DataGridLessonColumnBuilder(WindowComponentHost windowComponentHost, LessonEntity currentLesson, LocalizationContainer localization) {
            _windowComponentHost = windowComponentHost;
            _currentLesson = currentLesson;
            _localization = localization;
        }

        public DataGridColumn BuildMissedLessonsColumn() {
            var dataGridTemplateColumn = new TextColumn {
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                Header = new TextBlock {Text = _localization["Пропуски"]},
                Binding = new Binding(nameof(StudentRowViewModel.MissedLessonsInfo)),
                IsReadOnly = true,
                CanUserSort = false
            };
            return dataGridTemplateColumn;
        }

        public DataGridColumn BuildAttestationSummaryColumn() {
            var dataGridTemplateColumn =
                new TextColumn {
                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                    Header = new TextBlock {
                        Text = _localization["Аттестация\n(общ)"]
                    },
                    Binding = new Binding(nameof(StudentRowViewModel.AttestationSummary)),
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

        public DataGridColumn BuildStudentLessonColumn(
            [NotNull] StudentLessonColumnHelper helper,
            [NotNull] LessonEntity lesson
        ) {
            var header = new Grid {
                Background = GetLessonColumnHeaderBackgroundColor(lesson),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var headerText = new TextBlock {
                Text = _localization["common.lesson.type." + lesson.LessonType] + "\n " +
                       lesson.Date?.ToString("dd.MM"),
                Foreground = lesson.Id == _currentLesson.Id ? Brushes.Red : Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(headerText);
            AddLessonNoteInfoToColumnHeader(header, lesson);
            var dataGridTemplateColumn = new StudentLessonColumn(helper, lesson) {
                Width = new DataGridLength(90),
                MinWidth = 50,
                Header = header,
                CanUserSort = false
            };
            var cellStyle = new Style {
                BasedOn = StudentLessonMarkCellStyle,
                TargetType = typeof(DataGridCell)
            };
            dataGridTemplateColumn.CellStyle = cellStyle;
            dataGridTemplateColumn.HeaderStyle = ColumnHeaderStyle;
            return dataGridTemplateColumn;
        }

        private void AddLessonNoteInfoToColumnHeader(Panel columnHeader, LessonEntity lesson) {
            var lessonNotes = lesson.Notes?.ToList();
            var headerContextMenu = columnHeader.ContextMenu = new ContextMenu();
            var openNotes = new MenuItem {
                Command = BuildOpenLessonNotesCommand(lesson),
                Header = _localization["Открыть заметки"]
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
                _windowComponentHost.AddPageAsync(new NoteListFormToken("Заметки", () => new LessonNote {
                    Lesson = lesson,
                    EntityId = lesson.Id
                }, lesson.Notes));
            });
        }
    }
}
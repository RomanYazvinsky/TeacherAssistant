using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper {
    public class CellBindings {
        public BindingBase MarkBinding { get; }
        public BindingBase BackgroundBinding { get; }
        public IEnumerable<MenuItem> ContextMenuItems { get; }

        public CellBindings(StudentLessonCellViewModel cell) {
            this.ContextMenuItems = BuildLessonCellContextMenu(cell);
            this.BackgroundBinding = new Binding {
                Source = cell,
                Path = new PropertyPath(nameof(StudentLessonCellViewModel.Color))
            };
            this.MarkBinding = new Binding {
                Source = cell,
                Path = new PropertyPath(nameof(StudentLessonCellViewModel.Mark))
            };
        }


        private static IEnumerable<MenuItem> BuildLessonCellContextMenu(StudentLessonCellViewModel cell) {
            var toggleItem = new MenuItem {
                Command = cell.ToggleRegistrationHandler,
                Header = "Отметить/пропуск"
            };
            var openItem = new MenuItem {
                Command = cell.OpenRegistrationHandler,
                Header = "Регистрация"
            };
            var openNotesItem = new MenuItem {
                Header = "Заметки",
                Command = cell.OpenNotesFormHandler
            };
            return new[] {toggleItem, openItem, openNotesItem};
        }
    }

    public class StudentLessonColumnHelper {
        private readonly Dictionary<StudentLessonCellViewModel, CellBindings> _cellBindings =
            new Dictionary<StudentLessonCellViewModel, CellBindings>();

        public DependencyPropertyChangedEventHandler CreateAsyncHandler(
            FrameworkElement cell,
            FrameworkElement textBlock,
            FrameworkElement icon,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                Application.Current.Dispatcher?.BeginInvoke(
                    () => UpdateCellValue(cell, textBlock, icon, args.NewValue as StudentRowViewModel, lessonId),
                    DispatcherPriority.Background
                );
            }

            return OnDataContextChanged;
        }

        public DependencyPropertyChangedEventHandler CreateEagerHandler(
            FrameworkElement cell,
            FrameworkElement textBlock,
            FrameworkElement icon,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                UpdateCellValue(cell, textBlock, icon, args.NewValue as StudentRowViewModel, lessonId);
            }

            return OnDataContextChanged;
        }

        public void UpdateCellValue(
            FrameworkElement cell,
            FrameworkElement block,
            FrameworkElement icon,
            [CanBeNull] StudentRowViewModel row,
            long lessonId
        ) {
            var cellContext = row?.LessonToLessonMark[lessonId];
            if (cellContext == default) {
                return;
            }

            CellBindings bindings;
            if (!_cellBindings.ContainsKey(cellContext)) {
                bindings = new CellBindings(cellContext);
                _cellBindings.Add(cellContext, bindings);
            }
            else {
                bindings = _cellBindings[cellContext];
            }

            block.SetBinding(TextBlock.TextProperty, bindings.MarkBinding);
            cell.SetBinding(Panel.BackgroundProperty, bindings.BackgroundBinding);
            if (cell.ContextMenu == null) {
                cell.ContextMenu = new ContextMenu();
            }

            cell.ContextMenu.Items.Clear();
            var items = bindings.ContextMenuItems;
            (items.FirstOrDefault()?.Parent as ContextMenu)?.Items.Clear();
            foreach (var menuItem in items) {
                cell.ContextMenu.Items.Add(menuItem);
            }

            icon.Visibility = cellContext.ShowNotesInfo ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
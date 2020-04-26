using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper {
    public class StudentLessonColumnHelper {
        private readonly Dictionary<StudentLessonCellViewModel, CellBindings> _cellBindings =
            new Dictionary<StudentLessonCellViewModel, CellBindings>();

        public DependencyPropertyChangedEventHandler CreateAsyncHandler(
            FrameworkElement panel,
            FrameworkElement textBlock,
            FrameworkElement icon,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                Application.Current.Dispatcher?.BeginInvoke(
                    () => UpdateCellValue((DataGridCell) sender, panel, textBlock, icon, args.NewValue as StudentRowViewModel, lessonId),
                    DispatcherPriority.Background
                );
            }

            return OnDataContextChanged;
        }

        public DependencyPropertyChangedEventHandler CreateEagerHandler(
            FrameworkElement panel,
            FrameworkElement textBlock,
            FrameworkElement icon,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                UpdateCellValue((DataGridCell) sender, panel, textBlock, icon, args.NewValue as StudentRowViewModel, lessonId);
            }

            return OnDataContextChanged;
        }

        public void UpdateCellValue(
            DataGridCell cell,
            FrameworkElement panel,
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
            cell.SetBinding(CellStyleExtensions.IsRegisteredProperty, bindings.IsRegisteredBinding);
            if (panel.ContextMenu == null) {
                panel.ContextMenu = new ContextMenu();
            }

            panel.ContextMenu.Items.Clear();
            var items = bindings.ContextMenuItems;
            (items.FirstOrDefault()?.Parent as ContextMenu)?.Items.Clear();
            foreach (var menuItem in items) {
                panel.ContextMenu.Items.Add(menuItem);
            }

            icon.Visibility = cellContext.ShowNotesInfo ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
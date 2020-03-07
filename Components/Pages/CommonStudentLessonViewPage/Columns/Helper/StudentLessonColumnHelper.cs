using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper {
    public class CellBindings {
        public BindingBase MarkBinding { get; }
        public BindingBase BackgroundBinding { get; }
        public ContextMenu ContextMenu { get; }

        public CellBindings(StudentLessonCellViewModel cell) {
            this.ContextMenu = BuildLessonCellContextMenu(cell);
            this.BackgroundBinding = new Binding {
                Source = cell,
                Path = new PropertyPath(nameof(StudentLessonCellViewModel.Color))
            };
            this.MarkBinding = new Binding {
                Source = cell,
                Path = new PropertyPath(nameof(StudentLessonCellViewModel.Mark))
            };
        }


        private static ContextMenu BuildLessonCellContextMenu(StudentLessonCellViewModel cell) {
            var menu = new ContextMenu();
            var toggleItem = new MenuItem {
                Command = cell.ToggleRegistrationHandler,
                Header = "Отметить/пропуск"
            };
            var openItem = new MenuItem {
                Command = cell.OpenRegistrationHandler,
                Header = "Регистрация"
            };
            var openNotesItem = new MenuItem {
                Header = "Заметки"
            };
            menu.Items.Add(toggleItem);
            menu.Items.Add(openItem);
            return menu;
        }
    }

    public class StudentLessonColumnHelper {
        private readonly Dictionary<StudentLessonCellViewModel, CellBindings> _cellBindings =
            new Dictionary<StudentLessonCellViewModel, CellBindings>();

        public DependencyPropertyChangedEventHandler CreateAsyncHandler(
            FrameworkElement textBlock,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                Application.Current.Dispatcher?.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action<TextBlock>(
                        x => { UpdateCellValue(x, args.NewValue as StudentRowViewModel, lessonId); }),
                    textBlock);
            }

            return OnDataContextChanged;
        }

        public DependencyPropertyChangedEventHandler CreateEagerHandler(
            FrameworkElement textBlock,
            long lessonId
        ) {
            void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args) {
                UpdateCellValue(textBlock, args.NewValue as StudentRowViewModel, lessonId);
            }

            return OnDataContextChanged;
        }

        public void UpdateCellValue(
            FrameworkElement block,
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
            block.SetBinding(TextBlock.BackgroundProperty, bindings.BackgroundBinding);
            block.ContextMenu = bindings.ContextMenu;
        }
    }
}
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper {
    public class CellBindings {
        public BindingBase MarkBinding { get; }
        public BindingBase IsRegisteredBinding { get; }
        
        public IEnumerable<MenuItem> ContextMenuItems { get; }

        public CellBindings(StudentLessonCellViewModel cellContext) {
            this.ContextMenuItems = BuildLessonCellContextMenu(cellContext);
            this.IsRegisteredBinding = new Binding {
                Source = cellContext,
                Path = new PropertyPath(nameof(StudentLessonCellViewModel.IsRegistered))
            };
            this.MarkBinding = new Binding {
                Source = cellContext,
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
}
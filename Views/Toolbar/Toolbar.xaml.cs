using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.Components.Table;
using TeacherAssistant.State;

namespace TeacherAssistant.Toolbar
{
    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class Toolbar : UserControl
    {
        public class Cmd : ICommand
        {
            private Action _action;
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public Cmd(Action action)
            {
                _action = action;
            }

            public void Execute(object parameter)
            {
                _action();
            }

            public event EventHandler CanExecuteChanged;
        }

        public Toolbar()
        {
            InitializeComponent();
            DataContext = new ToolbarModel();
            var btn = new Button();
            btn.Content = "Назад";
            btn.VerticalAlignment = VerticalAlignment.Stretch;
            btn.VerticalContentAlignment = VerticalAlignment.Center;
            btn.Command = new Cmd(() =>
            {
                var parent = (Grid)Parent;
                var layoutStateManagement =
                LayoutStateManagement.GetInstance();
                layoutStateManagement.AttachedPluginStore
                    .Dispatch(new LayoutStateManagement.DetachAll());
                SideEffectManager.RemoveSideEffect("UpdateDatabaseOnStudentRegistration");
                string lessonTableId = "LessonsList";
                Publisher.Publish<StudentModel>(lessonTableId + ".SelectedItem", null);
                SideEffectManager.RemoveSideEffect("OnChangeReaderStatusAllowAutoregister");
                layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachGenericView(lessonTableId, "Table", parent,
                        new ViewConfig { ColumnSize = 62, RowSize = 32, Row = 3, Column = 1 }, typeof(LessonModel)));
                layoutStateManagement.AttachedPluginStore.Dispatch(
                    new LayoutStateManagement.AttachView("Toolbar", "Toolbar", parent,
                        new ViewConfig { ColumnSize = 64, RowSize = 2 }));
            });
            Bar.Items.Add(btn);
        }
    }
}

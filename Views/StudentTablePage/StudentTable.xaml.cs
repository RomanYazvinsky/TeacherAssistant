using System.Windows.Controls;
using System.Windows.Input;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.StudentTable
{
    /// <summary>
    /// Interaction logic for StudentTable.xaml
    /// </summary>
    public partial class StudentTable : UserControl
    {

        private StudentTableModel _model;
        public StudentTable(string id)
        {
            InitializeComponent();
            _model = Injector.GetInstance().Kernel.Get<StudentTableModel>();
            _model.Init(id);
            DataContext = _model;

            SortHelper.AddColumnSorting(Students);
        }

        private void LessonStudentsList_OnKeyDown(object sender, KeyEventArgs e)
        {
          //  throw new NotImplementedException();
        }

        private void List_OnMouseLeave(object sender, MouseEventArgs e)
        {
            // throw new NotImplementedException();
        }

        private void List_OnMouseEnter(object sender, MouseEventArgs e)
        {
            // throw new NotImplementedException();
        }

        private void OnLessonStudentsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           // throw new NotImplementedException();
        }

        private void OnLessonStudentsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          //  throw new NotImplementedException();
        }
    }
}

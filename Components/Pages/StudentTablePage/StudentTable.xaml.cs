using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.StudentTable
{
    /// <summary>
    /// Interaction logic for StudentTable.xaml
    /// </summary>
    public partial class StudentTable : View<StudentTableModel>
    {
        public StudentTable(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }
    }
}
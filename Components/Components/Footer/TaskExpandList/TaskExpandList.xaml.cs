using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Footer.TaskExpandList
{
    /// <summary>
    /// Interaction logic for TaskExpandList.xaml
    /// </summary>
    public partial class TaskExpandList : View<TaskExpandListModel>
    {
        private TaskExpandListModel _model;
        public TaskExpandList(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }
    }
}

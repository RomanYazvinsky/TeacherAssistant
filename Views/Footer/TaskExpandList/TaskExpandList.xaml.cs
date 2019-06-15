using System.Windows;
using System.Windows.Controls;

namespace TeacherAssistant.Footer.TaskExpandList
{
    /// <summary>
    /// Interaction logic for TaskExpandList.xaml
    /// </summary>
    public partial class TaskExpandList : UserControl
    {
        private TaskExpandListModel _model;
        public TaskExpandList()
        {
            InitializeComponent();
            _model = new TaskExpandListModel();
            DataContext = _model;
            _model.Init("");
        }
    }
}

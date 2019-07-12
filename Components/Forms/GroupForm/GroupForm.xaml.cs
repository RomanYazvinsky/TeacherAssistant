using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Forms.GroupForm
{
    /// <summary>
    /// Interaction logic for GroupForm.xaml
    /// </summary>
    public partial class GroupForm : View<GroupFormModel>
    {
        public GroupForm(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }
    }
}

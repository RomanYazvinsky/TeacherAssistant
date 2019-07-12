using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Pages.LessonForm
{
    /// <summary>
    /// Interaction logic for LessonForm.xaml
    /// </summary>
    public partial class LessonForm : View<LessonFormModel>
    {
        public LessonForm(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }
    }
}

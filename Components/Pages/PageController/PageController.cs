using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Pages {
    public class PageControllerBase : View<PageControllerModel> {
    }

    /// <summary>
    /// Interaction logic for MainWindowPage.xaml
    /// </summary>
    public partial class PageController : PageControllerBase {
        public PageController() {
            InitializeComponent();
        }
    }
}
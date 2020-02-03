using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Pages {
    public class PageControllerBase : View<PageControllerToken, PageControllerModel> {
    }

    /// <summary>
    /// Interaction logic for MainWindowPage.xaml
    /// </summary>
    public partial class PageController : PageControllerBase {
        public PageController(PageControllerEffects effects) {
            InitializeComponent();
        }
    }
}
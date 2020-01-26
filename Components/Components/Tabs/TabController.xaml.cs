using System.Windows.Controls;
using Dragablz;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components.Tabs {

    public class TabControllerToken : PageModuleToken<TabControllerModule> {
        public IModuleToken InitialModuleToken { get; }

        public TabControllerToken(IModuleToken token) : base("") {
            this.InitialModuleToken = token;
        }
    }

    public class TabControllerModule : Module {
        public TabControllerModule()
            : base(new[] {
                typeof(TabController),
                typeof(TabControllerModel),
                typeof(TabPageHost)
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<TabController>();
        }
    }

    public class TabControllerBase : View<TabControllerModel> {
    }

    /// <summary>
    /// Interaction logic for TabManager.xaml
    /// </summary>
    ///
    public partial class TabController : TabControllerBase {
        public TabController() {
            InitializeComponent();
        }

        private void OpenTabOnHover(object sender, object e) {
            TabablzControl.SelectedItem = ((DragablzItem) sender).Content;
        }
    }
}
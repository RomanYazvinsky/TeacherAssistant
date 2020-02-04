using Dragablz;
using Grace.DependencyInjection;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components.Tabs {
    public class TabControllerToken : PageModuleToken<TabControllerModule> {

        public TabControllerToken() : base("") {
        }
    }

    public class TabControllerModule : SimpleModule {
        public TabControllerModule() : base(typeof(TabController)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<TabControllerModel>();
            block.ExportModuleScope<TabPageHost>();
            block.ExportModuleScope<ModuleActivator>();
            block.ExportModuleScope<TabController>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class TabControllerBase : View<TabControllerToken, TabControllerModel> {
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

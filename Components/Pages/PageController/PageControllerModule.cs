using System.Windows.Controls;
using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages {
    public class PageControllerModule : SimpleModule {
        public PageControllerModule() : base(typeof(PageController)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<PageController>()
                .ImportProperty(controller => controller.ModuleToken)
                .ImportProperty(controller => controller.ViewModel);
            block.ExportModuleScope<PageControllerModel>();
            block.ExportModuleScope<PageControllerReducer>();
            block.ExportModuleScope<PageControllerEffects>();
            block.ExportModuleScope<ModuleActivator>();
        }
    }
}

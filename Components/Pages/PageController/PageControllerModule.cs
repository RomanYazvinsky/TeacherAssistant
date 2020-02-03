using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages {
    public class PageControllerModule : SimpleModule {
        public PageControllerModule() : base(typeof(PageController)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<PageController>(this.ModuleToken.Id)
                .ImportProperty(controller => controller.ModuleToken)
                .ImportProperty(controller => controller.ViewModel);
            block.ExportModuleScope<PageControllerModel>(this.ModuleToken.Id);
            block.ExportModuleScope<PageControllerReducer>(this.ModuleToken.Id);
            block.ExportModuleScope<PageControllerEffects>(this.ModuleToken.Id);
        }
    }
}
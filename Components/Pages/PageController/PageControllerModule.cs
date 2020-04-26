using Grace.DependencyInjection;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages.PageController {
    public class PageControllerModule : SimpleModule {
        public PageControllerModule() : base(typeof(PageController)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<PageController>()
                .ImportProperty(controller => controller.ModuleToken)
                .ImportProperty(controller => controller.ViewModel);
            block.DeclareComponent<PageControllerModel>();
            block.DeclareComponent<PageControllerReducer>();
            block.DeclareComponent<PageControllerEffects>();
            block.DeclareComponent<ModuleActivator>();
        }
    }
}

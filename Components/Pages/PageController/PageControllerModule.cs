using System.Windows.Controls;
using Ninject;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages {
    public class PageControllerModule : Module {
        public PageControllerModule()
            : base(new[] {
                typeof(PageController),
                typeof(PageControllerReducer),
                typeof(PageControllerModel),
                typeof(ModuleLoader)
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<PageController>();
        }
    }
}
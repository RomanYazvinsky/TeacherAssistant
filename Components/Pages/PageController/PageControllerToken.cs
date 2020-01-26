using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages {
    public class PageControllerToken : PageModuleToken<PageControllerModule> {
        public PageControllerToken(string title) : base(IdGenerator.GenerateId(), title) {
        }
    }
}
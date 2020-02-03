using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Modules.MainModule {
    public class MainModuleToken : PageModuleToken<MainModule> {
        public MainModuleToken(string title) : base(title) {
        }

        public bool ExitOnClose { get; set; } = false;
    }
}
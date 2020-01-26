using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Modules.MainModule {
    public class MainModuleToken : PageModuleToken<MainModule> {
        public const string GlobalModuleIdentifier = "Global";

        public MainModuleToken(string title) : base(GlobalModuleIdentifier, title) {
        }

        public bool ExitOnClose { get; set; } = false;
    }
}
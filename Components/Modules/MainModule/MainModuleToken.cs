using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Modules.MainModule {
    public class MainModuleToken : PageModuleToken<MainModule> {
        public MainModuleToken(string title) : base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }
}

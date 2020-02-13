using JetBrains.Annotations;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Pages {
    public class PageControllerToken : PageModuleToken<PageControllerModule> {
        public IModuleToken ContentToken { get; }
        [CanBeNull] public SimpleModule Content { get; }
        public PageControllerToken([NotNull] IModuleToken contentToken, SimpleModule content = null) : base("") {
            ContentToken = contentToken;
            Content = content;
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 768,
            InitialWidth = 1360
        };
    }
}

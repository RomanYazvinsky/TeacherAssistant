using System.Threading.Tasks;
using System.Windows.Controls;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components {

    public enum PageHostType {
        Tab,
        Window
    }

    public interface IPageHost {
        string Id { get; }

        PageHostType Type { get; }

        Task<TModule> AddPageAsync<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : SimpleModule;

        SimpleModule AddPage<TActivation>(TActivation activation)
            where TActivation : IModuleToken;

        Task<SimpleModule> AddPageAsync<TActivation>(TActivation activation)
            where TActivation : IModuleToken;

        void ClosePage(string id);

        bool ContainsPage(string id);

        Control Attach(SimpleModule module);

        SimpleModule Detach(IModuleToken token);
    }
}

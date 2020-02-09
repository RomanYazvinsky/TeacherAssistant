using System.Threading.Tasks;
using System.Windows.Controls;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components {

    public interface IPageHost {
        Task<TModule> AddPageAsync<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : SimpleModule;

        Task<SimpleModule> AddPageAsync<TActivation>(TActivation activation)
            where TActivation : IModuleToken;

        void ClosePage(string id);

        Control Attach<TModule>(TModule module) where TModule : SimpleModule;

        Control Detach<TModule>(TModule module) where TModule : SimpleModule;
    }
}

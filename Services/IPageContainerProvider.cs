using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components {

    public interface IPageHost {
        TModule AddPage<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : Module;

        Module AddPage<TActivation>(TActivation activation)
            where TActivation : IModuleToken;

        void ClosePage(string id);
    }
}
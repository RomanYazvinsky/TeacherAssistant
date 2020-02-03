using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Components {

    public interface IPageHost {
        TModule AddPage<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : SimpleModule;

        SimpleModule AddPage<TActivation>(TActivation activation)
            where TActivation : IModuleToken;

        void ClosePage(string id);

        void Attach<TModule>(TModule module) where TModule : SimpleModule;
    }
}
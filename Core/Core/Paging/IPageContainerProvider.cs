using System.Threading.Tasks;
using System.Windows.Controls;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Services.Paging
{
    public enum ComponentHostType
    {
        Tab,
        Window
    }

    public interface IComponentHost
    {
        string Id { get; }

        ComponentHostType Type { get; }

        SimpleModule AddPage<TToken>(TToken activation)
            where TToken : class, IModuleToken;

        Task<SimpleModule> AddPageAsync<TToken>(TToken activation)
            where TToken : class, IModuleToken;

        void ClosePage(string id);

        bool ContainsPage(string id);

        Control Attach(SimpleModule module);

        SimpleModule Detach(IModuleActivation module);
    }
}
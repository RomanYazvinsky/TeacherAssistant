using Ninject;
using Ninject.Extensions.Conventions;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.State
{
    public class Injector
    {
        public IKernel Kernel { get; }
        private static Injector _instance;
        private Injector()
        {
            Kernel = new StandardKernel();
            Kernel.Bind(syntax => syntax.FromAssembliesMatching(".").SelectAllClasses()
                .InheritedFrom<ISerialUtil>().BindDefaultInterface().Configure(onSyntax => onSyntax.InSingletonScope()));
        }

        public static Injector GetInstance()
        {
            return _instance ?? (_instance = new Injector());
        }
    }
}
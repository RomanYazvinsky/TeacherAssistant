using System.Linq;
using Ninject;
using Ninject.Extensions.ChildKernel;
using Ninject.Modules;
using Ninject.Parameters;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.State {
    public class Injector {
        public IKernel Kernel { get; }
        private static Injector _instance;

        private Injector() {
            this.Kernel = new StandardKernel();
        }

        public static Injector Instance => _instance ?? (_instance = new Injector());


        public static T Get<T>(params (string, object)[] constructorArgs) {
            return Instance.Kernel.Get<T>
            (
                constructorArgs.Select((key) => new ConstructorArgument(key.Item1, key.Item2)).ToArray()
            );
        }
       
    }
}
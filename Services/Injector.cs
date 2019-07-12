using System.Linq;
using Ninject;
using Ninject.Parameters;

namespace TeacherAssistant.State
{
    public class Injector
    {
        public IKernel Kernel { get; }
        private static Injector _instance;
        private Injector()
        {
            this.Kernel = new StandardKernel();
            Bind();
        }

        public static Injector Instance => _instance ?? (_instance = new Injector());

        private void Bind()
        {
        }

        public static T Get<T>(params (string, object)[] constructorArgs)
        {
            return Instance.Kernel.Get<T>
            (
                constructorArgs.Select((key) => new ConstructorArgument(key.Item1, key.Item2)).ToArray()
            );
        }

    }
}
using Ninject;

namespace TeacherAssistant.State
{
    public class Injector
    {
        public IKernel Kernel { get; }
        private static Injector _instance;
        private Injector()
        {
            Kernel = new StandardKernel();
        }

        public static Injector GetInstance()
        {
            return _instance ?? (_instance = new Injector());
        }
    }
}
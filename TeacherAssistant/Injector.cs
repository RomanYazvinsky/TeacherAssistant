using System;
using System.Reactive.Linq;
using System.Reflection;
using Dao;
using Ninject;
using Ninject.Modules;
using TeacherAssistant.State;

namespace TeacherAssistant
{
    public class Injector : NinjectModule
    {
        public override void Load()
        {

            IKernel kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
         
        }
    }
}
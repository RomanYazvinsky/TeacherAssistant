using System.Collections.Generic;
using System.Collections.Immutable;
using Ninject;
using Ninject.Extensions.Conventions;

namespace Views.StudentList
{
    public class ViewComponentFactoriesModule
    {
        private static ViewComponentFactoriesModule _viewComponentFactoriesModule;
        public ImmutableList<AbstractViewComponentFactory> ViewComponentFactories { get; }

        private ViewComponentFactoriesModule()
        {
            IKernel kernel = new StandardKernel();
            kernel.Bind(scan => scan.FromAssemblyContaining<AbstractViewComponentFactory>()
                .SelectAllClasses()
                .InheritedFrom<AbstractViewComponentFactory>()
                .BindAllBaseClasses());
            ViewComponentFactories = new List<AbstractViewComponentFactory>(kernel.GetAll<AbstractViewComponentFactory>()).ToImmutableList();
        }

        public static ViewComponentFactoriesModule GetInstance()
        {
            if (_viewComponentFactoriesModule == null)
            {
                _viewComponentFactoriesModule = new ViewComponentFactoriesModule();
            }

            return _viewComponentFactoriesModule;
        }
    }
}
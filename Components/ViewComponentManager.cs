using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ninject;
using Ninject.Extensions.Conventions;

namespace TeacherAssistant.Components
{
    public class ViewComponentFactoriesModule
    {
        private static ViewComponentFactoriesModule _viewComponentFactoriesModule;
        public ImmutableList<AbstractViewComponentFactory> ViewComponentFactories { get; }
        public ImmutableList<GenericViewComponentFactory> GenericViewComponentFactories { get; }
        private ViewComponentFactoriesModule()
        {
            IKernel abstractSearch = new StandardKernel();
            abstractSearch.Bind(scan => scan.From("TeacherAssistant.Components.Impl")
                .SelectAllClasses()
                .InheritedFrom<AbstractViewComponentFactory>()
                .BindAllBaseClasses());
            ViewComponentFactories = new List<AbstractViewComponentFactory>(abstractSearch.GetAll<AbstractViewComponentFactory>()).ToImmutableList();
            IKernel genericSearch = new StandardKernel();
            genericSearch.Bind(scan => scan.From("TeacherAssistant.Components.Impl")
                .SelectAllClasses()
                .InheritedFrom<GenericViewComponentFactory>()
                .BindAllBaseClasses());
            GenericViewComponentFactories = new List<GenericViewComponentFactory>(genericSearch.GetAll<GenericViewComponentFactory>()).ToImmutableList();
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
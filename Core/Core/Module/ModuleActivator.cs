using System;
using System.Threading.Tasks;
using Grace.DependencyInjection;

namespace TeacherAssistant.Core.Module
{
    public class ModuleActivator
    {
        private readonly IInjectionScope _container;

        public ModuleActivator(IInjectionScope container)
        {
            _container = container;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="token"></param>
        /// <typeparam name="TActivationToken"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException">if token does not provide acceptable module type</exception>
        public Task<SimpleModule> ActivateAsync<TActivationToken>(TActivationToken token)
            where TActivationToken : IModuleToken
        {
            var type = token.ModuleType;

            return Task.Run(() =>
            {
                if (!type.IsSubclassOf(typeof(SimpleModule)))
                {
                    throw new ArgumentException("Token does not belong to any module");
                }

                var injectionScope = _container.CreateChildScope(block =>
                    {
                        block.ExportInstance(token)
                            .As<IModuleToken>()
                            .As<TActivationToken>()
                            .Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName);
                        block.ExportModuleScope(type)
                            .ImportProperty(nameof(SimpleModule.Injector));
                    }
                    , token.Id);

                var lifecycleModule = (SimpleModule) injectionScope.Locate(type);
                injectionScope.Configure(block =>
                {
                    block.AddModule(lifecycleModule);
                });
                // var whatDoIHave = injectionScope.WhatDoIHave();
                // Debug.WriteLine(whatDoIHave);
                SetupDestructor(injectionScope, token);
                return lifecycleModule;
            });
        }

        private static void SetupDestructor(IDisposable scope, IModuleToken token)
        {
            void Handler(object sender, object args)
            {
                scope.Dispose();
                token.Deactivated -= Handler;
            }

            token.Deactivated += Handler;
        }
    }

    public static class ExportExtensions
    {
        public static IFluentExportStrategyConfiguration<T> ExportModuleScope<T>(
            this IExportRegistrationBlock block)
        {
            return block.Export<T>().As<T>().Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName);
        }

        public static IFluentExportStrategyConfiguration ExportModuleScope(
            this IExportRegistrationBlock block, Type type)
        {
            return block.Export(type).As(type).Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName);
        }
    }
}

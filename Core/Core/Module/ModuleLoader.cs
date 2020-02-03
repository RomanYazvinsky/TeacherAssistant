using System;
using Grace.DependencyInjection;

namespace TeacherAssistant.Core.Module {
    public class ModuleLoader {
        private readonly IExportLocatorScope _container;

        public ModuleLoader(IExportLocatorScope container) {
            _container = container;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <typeparam name="TActivationToken"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException">if token does not provide acceptable module type</exception>
        public SimpleModule Activate<TActivationToken>(TActivationToken token)
            where TActivationToken : IModuleToken {
            var type = token.ModuleType;
            if (!type.IsSubclassOf(typeof(SimpleModule))) {
                throw new ArgumentException("Token does not belong to any module");
            }

            var injectionScope = _container.GetInjectionScope().CreateChildScope(block => {
                    block.ExportInstance(token).As<IModuleToken>().As<TActivationToken>().Lifestyle.SingletonPerNamedScope(token.Id);
                    block.ExportModuleScope(type, token.Id)
                        .ImportProperty(nameof(SimpleModule.Injector))
                        .ImportProperty(nameof(SimpleModule.ModuleToken));
                }
            , token.Id);

            var lifecycleModule = (SimpleModule) injectionScope.Locate(type);
            injectionScope.Configure(block => { block.AddModule(lifecycleModule); });
            // var whatDoIHave = injectionScope.WhatDoIHave();
            // Debug.WriteLine(whatDoIHave);
            SetupDestructor(injectionScope, token);
            return lifecycleModule;
        }

        private static void SetupDestructor(IDisposable kernel, IModuleToken token) {
            void Handler(object sender, object args) {
                kernel.Dispose();
                token.Deactivated -= Handler;
            }

            token.Deactivated += Handler;
        }
    }

    public static class ExportExtensions {
        public static IFluentExportStrategyConfiguration<T> ExportModuleScope<T>(
            this IExportRegistrationBlock block, string id) {
            return block.Export<T>().Lifestyle.SingletonPerNamedScope(id);
        }

        public static IFluentExportStrategyConfiguration ExportModuleScope(
            this IExportRegistrationBlock block, Type type, string id) {
            return block.Export(type).Lifestyle.SingletonPerNamedScope(id);
        }
    }
}
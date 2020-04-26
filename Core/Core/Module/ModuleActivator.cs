using System;
using System.Threading.Tasks;
using Grace.DependencyInjection;
using TeacherAssistant.State;

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
            where TActivationToken : class, IModuleToken
        {
            return Task.Run(() => Activate(token));
        }

        public SimpleModule Activate<TActivationToken>(TActivationToken token)
            where TActivationToken : class, IModuleToken
        {
            var type = token.ModuleType;


            // typeOf() => interface IModuleToken
            // GetType() => class SomeModuleToken
            var tokenType = token.GetType();
            var genericType = typeof(ModuleActivation<>).MakeGenericType(tokenType);
            var ctor = genericType.GetConstructor(new[] {typeof(string), tokenType});
            var newModuleInstanceId = IdGenerator.GenerateId();
            var activatedToken = (IModuleActivation) ctor.Invoke(new object[] {newModuleInstanceId, token});
            var injectionScope = _container.CreateChildScope(block =>
            {
                block.ExportInstance(activatedToken)
                    .As<IModuleActivation>()
                    .As(genericType)
                    .Lifestyle.SingletonPerNamedScope(newModuleInstanceId);
                block.ActivateModule(type);
            }, newModuleInstanceId);

            var module = (SimpleModule) injectionScope.Locate(type);
            injectionScope.Configure(block => { block.AddModule(module); });
            SetupDestructor(injectionScope, activatedToken);
            return module;
        }

        private static void SetupDestructor(IDisposable scope, IModuleActivation activated)
        {
            void Handler(object sender, object args)
            {
                scope.Dispose();
                activated.Deactivated -= Handler;
            }

            activated.Deactivated += Handler;
        }
    }

    public static class ExportExtensions
    {
        public static IFluentExportInstanceConfiguration<T> UseService<T>(
            this IExportRegistrationBlock block, T service
        )
        {
            return block.ExportInstance(service).Lifestyle.Singleton();
        }

        public static IFluentExportStrategyConfiguration<T> RequireService<T>(
            this IExportRegistrationBlock block, bool useExisting = true
        )
        {
            return useExisting
                ? block.Export<T>().As<T>().Lifestyle.Singleton().IfNotRegistered(typeof(T))
                : block.DeclareComponent<T>();
        }

        public static IFluentExportStrategyConfiguration<T> DeclareComponent<T>(
            this IExportRegistrationBlock block
        )
        {
            return block.Export<T>().As<T>().Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName);
        }

        public static IFluentExportStrategyConfiguration DeclareComponent(
            this IExportRegistrationBlock block, Type type
        )
        {
            return block.Export(type).As(type).Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName);
        }

        public static IFluentExportStrategyConfiguration ActivateModule(
            this IExportRegistrationBlock block, Type type
        )
        {
            return block.Export(type)
                .As(type)
                .Lifestyle.SingletonPerNamedScope(block.OwningScope.ScopeName)
                .ImportProperty(nameof(SimpleModule.Injector));
        }
    }
}
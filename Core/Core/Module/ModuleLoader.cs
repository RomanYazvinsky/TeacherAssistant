using System;
using Ninject;
using Ninject.Extensions.ChildKernel;

namespace TeacherAssistant.Core.Module {
    public class ModuleLoader {
        private readonly IKernel _kernel;

        public ModuleLoader(IKernel kernel) {
            _kernel = kernel;
        }

        public TModule Activate<TModule, TActivationToken>(TActivationToken token)
            where TModule : Module
            where TActivationToken : PageModuleToken<TModule> {
            var childKernel = new ChildKernel(_kernel);

            childKernel.Bind<TActivationToken>().ToConstant(token);
            childKernel.Bind<IModuleToken>().ToConstant(token);
            childKernel.Bind<TModule>().ToSelf();
            var lifecycleModule = childKernel.Get<TModule>();
            childKernel.Load(lifecycleModule);
            SetupDestructor(childKernel, token);
            return lifecycleModule;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <typeparam name="TActivationToken"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException">if token does not provide acceptable module type</exception>
        public Module Activate<TActivationToken>(TActivationToken token)
            where TActivationToken : IModuleToken {
            var type = token.ModuleType;
            if (!type.IsSubclassOf(typeof(Module))) {
                throw new ArgumentException("Token does not belong to any module");
            }

            var childKernel = new ChildKernel(_kernel);
            childKernel.Rebind<IModuleToken>().ToConstant(token);
            childKernel.Bind<TActivationToken>().ToConstant(token);
            childKernel.Bind(token.ModuleType).ToSelf();
            var lifecycleModule = (Module) childKernel.Get(token.ModuleType);
            childKernel.Load(lifecycleModule);
            SetupDestructor(childKernel, token);
            return lifecycleModule;
        }
        private static void SetupDestructor(IKernel kernel, IModuleToken token) {
            void Handler(object sender, object args) {
                kernel.Dispose();
                token.Deactivated -= Handler;
            }

            token.Deactivated += Handler;
        }
    }
}
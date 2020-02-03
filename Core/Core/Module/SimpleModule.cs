using System;
using System.Collections.Immutable;
using System.Windows.Controls;
using Containers.Annotations;
using Grace.DependencyInjection;

namespace TeacherAssistant.Core.Module {
    using GlobalState = ImmutableDictionary<string, object>;

    public interface IInitializable {
        void Initialize();
    }
    
    public abstract class SimpleModule : IConfigurationModule, IEntryPoint {
        private readonly Type _entryType;

        protected SimpleModule(Type entryType) {
            _entryType = entryType;
        }

        public IExportLocatorScope Injector { get; set; }
        public IModuleToken ModuleToken { get; set; }
 

        public virtual Control GetEntryComponent() {
            return this.Injector?.Locate(_entryType) as Control
                   ?? throw new ArgumentException("injector is not set!");
        }

        [CanBeNull]
        public virtual IModuleToken GetToken() {
            return this.Injector?.Locate<IModuleToken>();
        }    

        public abstract void Configure(IExportRegistrationBlock registrationBlock);
    }
}
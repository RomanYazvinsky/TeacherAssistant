using System;
using System.Collections.Immutable;
using System.Windows.Controls;
using Grace.DependencyInjection;
using JetBrains.Annotations;

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


        public virtual Control GetEntryComponent()
        {
            return this.Injector?.Locate(_entryType) as Control;
        }

        [CanBeNull]
        public virtual IModuleToken GetToken() {
            return this.Injector?.Locate<IModuleToken>();
        }

        public abstract void Configure(IExportRegistrationBlock registrationBlock);
    }
}

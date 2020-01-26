using System;
using System.Collections.Immutable;
using System.Windows.Controls;
using Ninject.Modules;

namespace TeacherAssistant.Core.Module {
    using GlobalState = ImmutableDictionary<string, object>;

    
    public abstract class Module : NinjectModule, IEntryPoint {
        private readonly Type[] _typesToLoad;

        public Module(Type[] typesToLoad) {
            _typesToLoad = typesToLoad;
        }

        public override void Load() {
            if (this.Kernel == null) {
                return;
            }

            foreach (var type in _typesToLoad) {
                Bind(type).ToSelf().InSingletonScope();
            }
        }


        public abstract Control GetEntryComponent();
    }
}
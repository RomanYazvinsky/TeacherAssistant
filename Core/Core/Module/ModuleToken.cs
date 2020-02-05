using System;
using Containers.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.Core.Module {
    public interface IModuleToken {
        event EventHandler Deactivated;
        void Deactivate();
        string Id { get; }
        string Title { get; }
        Type ModuleType { get; }
    }

    public abstract class PageModuleToken<TModule> : IModuleToken where TModule : SimpleModule {
        public string Title { get; }
        [NotNull] public string Id { get; }
        public Type ModuleType => typeof(TModule);

        protected PageModuleToken(string title){
            this.Id = IdGenerator.GenerateId();
            this.Title = title;
        }

        public event EventHandler Deactivated;
        public void Deactivate() {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }
    }
}

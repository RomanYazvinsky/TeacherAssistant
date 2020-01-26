using System;
using TeacherAssistant.State;

namespace TeacherAssistant.Core.Module {
    public interface IModuleToken {
        event EventHandler Deactivated;
        void Deactivate();
        string Id { get; }
        string Title { get; }
        Type ModuleType { get; }
    }
    
    public abstract class PageModuleToken<TModule> : IModuleToken where TModule : Module {
        public string Title { get; }
        public string Id { get; }
        public Type ModuleType => typeof(TModule);

        protected PageModuleToken(string title): this(IdGenerator.GenerateId(), title) {
        }
        protected PageModuleToken(string id, string title) {
            this.Id = id;
            this.Title = title;
        }

        public event EventHandler Deactivated;
        public void Deactivate() {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }
    }
}
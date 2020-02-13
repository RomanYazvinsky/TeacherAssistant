using System;
using JetBrains.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.Core.Module {
    public class PageProperties {
        public double InitialWidth { get; set; }
        public double InitialHeight { get; set; }
    }

    public interface IModuleToken {
        event EventHandler Deactivated;
        void Deactivate();
        string Id { get; }
        string Title { get; }
        [NotNull] Type ModuleType { get; }
        [NotNull] PageProperties PageProperties { get; }
    }

    public abstract class PageModuleToken<TModule> : IModuleToken where TModule : SimpleModule {
        public string Title { get; }
        [NotNull] public string Id { get; }
        public Type ModuleType => typeof(TModule);
        public abstract PageProperties PageProperties { get; }

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

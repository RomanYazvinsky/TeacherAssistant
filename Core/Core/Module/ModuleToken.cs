using System;
using JetBrains.Annotations;

namespace TeacherAssistant.Core.Module {
    public class PageProperties {
        public double InitialWidth { get; set; }
        public double InitialHeight { get; set; }
    }

    public interface IModuleToken {
        string Title { get; }
        [NotNull] Type ModuleType { get; }
        [NotNull] PageProperties PageProperties { get; }
    }

    public abstract class PageModuleToken<TModule> : IModuleToken where TModule : SimpleModule {
        public string Title { get; }
        public Type ModuleType => typeof(TModule);
        public abstract PageProperties PageProperties { get; }

        protected PageModuleToken(string title) {
            this.Title = title;
        }
    }
}
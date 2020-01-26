using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Ninject.Infrastructure.Language;
using TeacherAssistant.Components;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant {
    public class PageInfo<T> where T : Control {
        public PageInfo(PageInfo<T> info, T container, Control page) :
            this(info.Id, container, page) {
            info.Next = this;
            this.Previous = info;
        }

        public PageInfo(string id, T container, Control page) {
            this.Id = id;
            this.Page = page;
            this.Container = container;
        }

        public string Id { get; set; }
        public Control Page { get; set; }
        public T Container { get; set; }
        public PageInfo<T> Previous { get; }
        public PageInfo<T> Next { get; set; }
    }

    public abstract class AbstractPageHost<TContainer> : IPageHost where TContainer : Control {
        private readonly ModuleLoader _loader;

        protected readonly Dictionary<string, PageInfo<TContainer>> Pages =
            new Dictionary<string, PageInfo<TContainer>>();

        protected AbstractPageHost(ModuleLoader loader) {
            _loader = loader;
        }

        public virtual TModule AddPage<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : Module {
            var module = _loader.Activate<TModule, TActivation>(activation);
            var control = module.GetEntryComponent();
            var pageInfo = new PageInfo<TContainer>(activation.Id,
                BuildContainer(activation, control), control);
            Pages.Add(activation.Id, pageInfo);
            return module;
        }

        public Module AddPage<TActivation>(TActivation activation) where TActivation : IModuleToken {
            var module = _loader.Activate(activation);
            var control = module.GetEntryComponent();
            var pageInfo = new PageInfo<TContainer>(activation.Id,
                BuildContainer(activation, control), control);
            Pages.Add(activation.Id, pageInfo);
            return module;
        }

        public abstract void ClosePage(string id);

        public IEnumerable<TContainer> CurrentPages => Pages.Values.Select(info => info.Container).ToEnumerable();

        public abstract TContainer BuildContainer<TActivation>(TActivation activation, Control control)
            where TActivation : IModuleToken;
    }
}
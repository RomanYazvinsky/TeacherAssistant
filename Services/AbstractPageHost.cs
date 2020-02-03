using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using TeacherAssistant.Components;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant {
    public class PageInfo<T> where T : Control {
        public PageInfo(PageInfo<T> info, T container, Control page, IModuleToken token) :
            this(info.Id, container, page, token) {
            info.Next = this;
            this.Previous = info;
        }

        public PageInfo(string id, T container, Control page, IModuleToken token) {
            this.Id = id;
            this.Page = page;
            this.Token = token;
            this.Container = container;
        }

        public string Id { get; set; }
        public Control Page { get; set; }
        public T Container { get; set; }
        public IModuleToken Token { get; }
        public PageInfo<T> Previous { get; }
        public PageInfo<T> Next { get; set; }
    }

    public abstract class AbstractPageHost<TContainer> : IPageHost where TContainer : Control {
        private readonly ModuleLoader _loader;

        public Dictionary<string, PageInfo<TContainer>> Pages { get; } =
            new Dictionary<string, PageInfo<TContainer>>();

        protected AbstractPageHost(ModuleLoader loader) {
            _loader = loader;
        }

        public virtual TModule AddPage<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : SimpleModule {
            var module = _loader.Activate(activation);
            Attach(module, activation);
            return (TModule) module;
        }

        public SimpleModule AddPage<TActivation>(TActivation activation) where TActivation : IModuleToken {
            var module = _loader.Activate(activation);
            Attach(module, activation);
            return module;
        }

        public abstract void ClosePage(string id);

        public virtual void Attach<TModule>(TModule module) where TModule : SimpleModule {
            Attach(module, module.GetToken());
        }

        protected virtual void Attach<TModule, TToken>(TModule module, TToken token)
            where TModule : SimpleModule
            where TToken : IModuleToken {
            var control = module.GetEntryComponent();
            var pageInfo = new PageInfo<TContainer>(token.Id,
                BuildContainer(token, control), control, token);
            this.Pages.Add(token.Id, pageInfo);
        }

        public IEnumerable<TContainer> CurrentPages => this.Pages.Values.Select(info => info.Container);

        public abstract TContainer BuildContainer<TActivation>(TActivation activation, Control control)
            where TActivation : IModuleToken;
    }
}
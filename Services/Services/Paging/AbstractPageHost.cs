using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using JetBrains.Annotations;
using NLog;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.Services.Paging {
    public class PageInfo<T> where T : Control {
        public PageInfo(PageInfo<T> info, T container, Control page, IModuleToken token, SimpleModule module) :
            this(info.Id, container, page, token, module) {
            info.Next = this;
            this.Previous = info;
        }

        public PageInfo(string id, T container, Control page, IModuleToken token, SimpleModule module) {
            this.Id = id;
            this.Page = page;
            this.Token = token;
            Module = module;
            this.Container = container;
        }

        public string Id { get; set; }
        public Control Page { get; set; }
        public T Container { get; set; }
        public IModuleToken Token { get; }

        public SimpleModule Module { get; }
        public PageInfo<T> Previous { get; }
        public PageInfo<T> Next { get; set; }
    }

    public abstract class AbstractPageHost<TContainer> : IPageHost where TContainer : Control {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ModuleActivator _activator;

        public Dictionary<string, PageInfo<TContainer>> Pages { get; } =
            new Dictionary<string, PageInfo<TContainer>>();

        protected AbstractPageHost(ModuleActivator activator) {
            _activator = activator;
        }

        public abstract string Id { get; }
        public abstract PageHostType Type { get; }

        public virtual async Task<TModule> AddPageAsync<TModule, TActivation>(TActivation activation)
            where TActivation : PageModuleToken<TModule>
            where TModule : SimpleModule {
            var module = await _activator.ActivateAsync(activation);
            Attach(module, activation);
            return (TModule) module;
        }

        public SimpleModule AddPage<TActivation>(TActivation activation) where TActivation : IModuleToken {
            var module = _activator.Activate(activation);
            Attach(module, activation);
            return module;
        }

        public async Task<SimpleModule> AddPageAsync<TActivation>(TActivation activation)
            where TActivation : IModuleToken {
            var module = await _activator.ActivateAsync(activation);
            Attach(module, activation);
            return module;
        }

        public abstract void ClosePage(string id);

        public bool ContainsPage(string id) {
            return this.Pages.ContainsKey(id);
        }

        public virtual Control Attach([NotNull] SimpleModule module) {
            return Attach(module, module.GetToken());
        }

        public SimpleModule Detach(IModuleToken token) {
            var id = token.Id;
            if (id == null) {
                throw new ArgumentException("id is null");
            }

            var pageInfo = this.Pages[id];
            UnregisterHandlers(token);
            this.Pages.Remove(id);
            return pageInfo.Module;
        }

        protected virtual TContainer Attach<TModule, TToken>(TModule module, TToken token)
            where TModule : SimpleModule
            where TToken : IModuleToken {
            Control control;
            try {
                control = module.GetEntryComponent();
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Error, "Cannot create page");
                Logger.Log(LogLevel.Error, e);
                throw;
            }

            var pageInfo = new PageInfo<TContainer>(token.Id,
                BuildContainer(token, control), control, token, module);
            this.Pages.Add(token.Id, pageInfo);
            return pageInfo.Container;
        }

        protected abstract void UnregisterHandlers(IModuleToken token);

        public IEnumerable<TContainer> CurrentPages => this.Pages.Values.Select(info => info.Container);

        public abstract TContainer BuildContainer<TActivation>(TActivation activation, Control control)
            where TActivation : IModuleToken;
    }
}

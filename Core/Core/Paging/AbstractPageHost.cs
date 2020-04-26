using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using JetBrains.Annotations;
using NLog;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Core.Paging
{
    public class ComponentHostContext<T> where T : Control
    {
        public ComponentHostContext(ComponentHostContext<T> info, T container, Control page,
            IModuleActivation activation,
            SimpleModule module) :
            this(info.Id, container, page, activation, module)
        {
            info.Next = this;
            this.Previous = info;
        }

        public ComponentHostContext(string id, T container, Control page, IModuleActivation activation,
            SimpleModule module)
        {
            this.Id = id;
            this.Page = page;
            this.Activation = activation;
            this.Module = module;
            this.Container = container;
        }

        public string Id { get; set; }
        public Control Page { get; set; }
        public T Container { get; set; }
        public IModuleActivation Activation { get; }

        public SimpleModule Module { get; }
        public ComponentHostContext<T> Previous { get; }
        public ComponentHostContext<T> Next { get; set; }
    }

    public abstract class AbstractComponentHost<TContainer> : IComponentHost where TContainer : Control
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ModuleActivator _activator;

        public Dictionary<string, ComponentHostContext<TContainer>> Pages { get; } =
            new Dictionary<string, ComponentHostContext<TContainer>>();

        protected AbstractComponentHost(ModuleActivator activator)
        {
            _activator = activator;
        }

        public abstract string Id { get; }
        public abstract ComponentHostType Type { get; }

        public SimpleModule AddPage<TActivation>(TActivation activation) where TActivation : class, IModuleToken
        {
            var module = _activator.Activate(activation);
            Attach(module);
            return module;
        }

        public async Task<SimpleModule> AddPageAsync<TActivation>(TActivation activation)
            where TActivation : class, IModuleToken
        {
            var module = await _activator.ActivateAsync(activation);
            Attach(module);
            return module;
        }

        public abstract void ClosePage(string id);

        public bool ContainsPage(string id)
        {
            return this.Pages.ContainsKey(id);
        }

        public virtual Control Attach([NotNull] SimpleModule module)
        {
            return Attach(module, module.GetActivation());
        }

        public SimpleModule Detach(IModuleActivation module)
        {
            var id = module.Id;
            if (id == null)
            {
                throw new ArgumentException("id is null");
            }

            var pageInfo = this.Pages[id];
            UnregisterHandlers(module);
            this.Pages.Remove(id);
            return pageInfo.Module;
        }

        protected virtual TContainer Attach<TModule, TToken>(TModule module, TToken token)
            where TModule : SimpleModule
            where TToken : IModuleActivation
        {
            Control control;
            try
            {
                control = module.GetEntryComponent();
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Cannot create page");
                Logger.Log(LogLevel.Error, e);
                throw;
            }

            var pageInfo = new ComponentHostContext<TContainer>(token.Id,
                BuildContainer(token, control), control, token, module);
            this.Pages.Add(token.Id, pageInfo);
            return pageInfo.Container;
        }

        protected abstract void UnregisterHandlers(IModuleActivation module);

        public IEnumerable<TContainer> CurrentPages => this.Pages.Values.Select(info => info.Container);

        public abstract TContainer BuildContainer<TActivation>(TActivation activation, Control control)
            where TActivation : IModuleActivation;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Paging;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant.Services.Paging
{
    class ModuleDestroyEventArgs : EventArgs
    {
        public ModuleDestroyEventArgs(Control container, IModuleActivation moduleActivation)
        {
            this.Container = container;
            this.ModuleActivation = moduleActivation;
        }

        public Control Container { get; }
        public IModuleActivation ModuleActivation { get; }
    }

    class WindowSubscriptionContainer : IDisposable
    {
        private readonly Window _window;
        private readonly IModuleActivation _activation;
        private readonly MainReducer _reducer;
        private readonly IDisposable _subscription;
        public event EventHandler<ModuleDestroyEventArgs> Disposed;

        public WindowSubscriptionContainer(
            Window window,
            IModuleActivation activation,
            MainReducer reducer
        )
        {
            _window = window;
            _activation = activation;
            _reducer = reducer;

            window.KeyDown += OnWindowOnKeyDown;
            activation.Deactivated += OuterDeactivationHandler;
            window.Closed += DeactivationHandler;
            _subscription = reducer.Select(state => state.FullscreenMode)
                .Subscribe(isFullscreen =>
                {
                    window.WindowStyle = isFullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                    window.WindowState = isFullscreen ? WindowState.Maximized : WindowState.Normal;
                });
        }

        private void OuterDeactivationHandler(object sender, EventArgs args)
        {
            _window.Close();
        }

        private void DeactivationHandler(object sender, EventArgs args)
        {
            Dispose();
        }

        private void OnWindowOnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.F11)
            {
                _reducer.Dispatch(new SetFullscreenModeAction());
            }
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _window.Closed -= DeactivationHandler;
            _activation.Deactivated -= OuterDeactivationHandler;
            _window.KeyDown -= OnWindowOnKeyDown;
            Disposed?.Invoke(this, new ModuleDestroyEventArgs(_window, _activation));
        }
    }

    public class WindowComponentHost : AbstractComponentHost<Window>
    {
        private readonly IModuleActivation _activation;
        private readonly MainReducer _reducer;
        public override string Id => _activation.Id;

        private readonly Dictionary<string, WindowSubscriptionContainer> _subscriptionContainers =
            new Dictionary<string, WindowSubscriptionContainer>();

        public override ComponentHostType Type => ComponentHostType.Window;

        protected override void UnregisterHandlers(IModuleActivation token)
        {
            var windowSubscriptionContainer = this._subscriptionContainers[token.Id];
            windowSubscriptionContainer.Disposed -= DestroyModule;
            windowSubscriptionContainer.Dispose();
            _subscriptionContainers.Remove(token.Id);
        }

        public override Window BuildContainer<TActivation>(TActivation activation, Control page)
        {
            var owner = this.Pages.Count == 0
                ? (KeyValuePair<string, ComponentHostContext<Window>>?) null
                : this.Pages.First();
            var window = new Window
            {
                Title = activation.GetToken().Title,
                Uid = activation.Id,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = activation.GetToken().PageProperties.InitialWidth,
                Height = activation.GetToken().PageProperties.InitialHeight,
            };
            if (owner != null)
            {
                window.Owner = owner.Value.Value.Container;
            }

            window.SetResourceReference(Control.BackgroundProperty, "PrimaryBackgroundBrush");
            var windowSubscriptionContainer = new WindowSubscriptionContainer(window, activation, _reducer);
            windowSubscriptionContainer.Disposed += DestroyModule;
            this._subscriptionContainers.Add(activation.Id, windowSubscriptionContainer);
            window.Show();
            return window;
        }

        public override void ClosePage(string id)
        {
            this.Pages[id].Container.Close();
            this.Pages.Remove(id);
        }

        private void DestroyModule(object sender, ModuleDestroyEventArgs args)
        {
            this.Pages.Remove(args.ModuleActivation.Id);
            args.ModuleActivation.Deactivate();
            if (this.Pages.Count == 0)
            {
                _activation.Deactivate();
            }
        }

        public WindowComponentHost(IModuleActivation activation, ModuleActivator activator, MainReducer reducer) :
            base(activator)
        {
            _activation = activation;
            _reducer = reducer;
        }
    }
}
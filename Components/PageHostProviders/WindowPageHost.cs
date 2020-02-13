using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeacherAssistant.Components;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant {
    class ModuleDestroyEventArgs : EventArgs {
        public ModuleDestroyEventArgs(Control container, IModuleToken token) {
            Container = container;
            Token = token;
        }

        public Control Container { get; }
        public IModuleToken Token { get; }
    }
    class WindowSubscriptionContainer : IDisposable {
        private readonly Window _window;
        private readonly IModuleToken _token;
        private readonly MainReducer _reducer;
        private readonly IDisposable _subscription;
        public event EventHandler<ModuleDestroyEventArgs> Disposed;

        public WindowSubscriptionContainer(
            Window window,
            IModuleToken token,
            MainReducer reducer
        ) {
            _window = window;
            _token = token;
            _reducer = reducer;

            window.KeyDown += OnWindowOnKeyDown;
            token.Deactivated += OuterDeactivationHandler;
            window.Closed += DeactivationHandler;
            _subscription = reducer.Select(state => state.FullscreenMode)
                .Subscribe(isFullscreen => {
                    window.WindowStyle = isFullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                    window.WindowState = isFullscreen ? WindowState.Maximized : WindowState.Normal;
                });
        }

        private void OuterDeactivationHandler(object sender, EventArgs args) {
            _window.Close();
        }

        private void DeactivationHandler(object sender, EventArgs args) {
            Dispose();
        }

        private void OnWindowOnKeyDown(object sender, KeyEventArgs args) {
            if (args.Key == Key.F11) {
                _reducer.Dispatch(new SetFullscreenModeAction());
            }
        }

        public void Dispose() {
            _subscription.Dispose();
            _window.Closed -= DeactivationHandler;
            _token.Deactivated -= OuterDeactivationHandler;
            _window.KeyDown -= OnWindowOnKeyDown;
            Disposed?.Invoke(this, new ModuleDestroyEventArgs(_window, _token));
        }
    }

    public class WindowPageHost : AbstractPageHost<Window> {
        private readonly IModuleToken _token;
        private readonly MainReducer _reducer;
        public override string Id => _token.Id;

        private readonly Dictionary<string, WindowSubscriptionContainer> _subscriptionContainers =
            new Dictionary<string, WindowSubscriptionContainer>();

        public override PageHostType Type => PageHostType.Window;

        protected override void UnregisterHandlers(IModuleToken token) {
            var windowSubscriptionContainer = this._subscriptionContainers[token.Id];
            windowSubscriptionContainer.Disposed -= DestroyModule;
            windowSubscriptionContainer.Dispose();
            _subscriptionContainers.Remove(token.Id);
        }

        public override Window BuildContainer<TActivation>(TActivation activation, Control page) {
            var owner = this.Pages.Count == 0
                ? (KeyValuePair<string, PageInfo<Window>>?) null
                : Pages.First();
            var window = new Window {
                Title = activation.Title,
                Uid = activation.Id,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = activation.PageProperties.InitialWidth,
                Height = activation.PageProperties.InitialHeight,
            };
            if (owner != null) {
                window.Owner = owner.Value.Value.Container;
            }
            window.SetResourceReference(Control.BackgroundProperty, "PrimaryBackgroundBrush");
            var windowSubscriptionContainer = new WindowSubscriptionContainer(window, activation, _reducer);
            windowSubscriptionContainer.Disposed += DestroyModule;
            this._subscriptionContainers.Add(activation.Id, windowSubscriptionContainer);
            window.Show();
            return window;
        }

        public override void ClosePage(string id) {
            this.Pages[id].Container.Close();
            this.Pages.Remove(id);
        }

        private void DestroyModule(object sender, ModuleDestroyEventArgs args) {
            this.Pages.Remove(args.Token.Id);
            args.Token.Deactivate();
            if (this.Pages.Count == 0) {
                _token.Deactivate();
            }
        }

        public WindowPageHost(IModuleToken token, ModuleActivator activator, MainReducer reducer) : base(activator) {
            _token = token;
            _reducer = reducer;
        }
    }
}

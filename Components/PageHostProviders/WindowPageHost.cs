using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant
{
    public class WindowPageHost : AbstractPageHost<Window>
    {
        private readonly IModuleToken _token;
        private readonly MainReducer _reducer;

        public override Window BuildContainer<TActivation>(TActivation activation, Control page)
        {
            var owner = this.Pages.Count == 0
                ? (KeyValuePair<string, PageInfo<Window>>?) null
                : Pages.First();
            var window = new Window
            {
                Title = activation.Title,
                Uid = activation.Id,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (owner != null)
            {
                window.Owner = owner.Value.Value.Container;
            }

            window.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.F11)
                {
                    _reducer.Dispatch(new SetFullscreenModeAction());
                }
            };

            var subscription = _reducer.Select(state => state.FullscreenMode)
                .Subscribe(isFullscreen =>
                {
                    window.WindowStyle = isFullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                });

            void OuterDeactivationHandler(object sender, EventArgs args)
            {
                window.Close();
            }

            void DeactivationHandler(object sender, EventArgs args)
            {
                subscription.Dispose();
                window.Closed -= DeactivationHandler;
                activation.Deactivated -= OuterDeactivationHandler;
                this.Pages.Remove(window.Uid);
                activation.Deactivate();
                if (this.Pages.Count == 0)
                {
                    _token.Deactivate();
                }
            }

            activation.Deactivated += OuterDeactivationHandler;
            window.Closed += DeactivationHandler;
            window.Show();
            return window;
        }

        public override void ClosePage(string id)
        {
            this.Pages[id].Container.Close();
            this.Pages.Remove(id);
        }

        public WindowPageHost(IModuleToken token, ModuleActivator activator, MainReducer reducer) : base(activator)
        {
            _token = token;
            _reducer = reducer;
        }
    }
}

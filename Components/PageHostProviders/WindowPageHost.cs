using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant
{
    public class WindowPageHost : AbstractPageHost<Window>
    {
        private readonly MainReducer _reducer;

        public override Window BuildContainer<TActivation>(TActivation activation, Control page)
        {
            var window = new Window
            {
                Uid = activation.Id,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
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
                this.Pages.Remove(((Window) sender).Uid);
                activation.Deactivate();
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

        public WindowPageHost(ModuleActivator activator, MainReducer reducer) : base(activator)
        {
            _reducer = reducer;
        }
    }
}

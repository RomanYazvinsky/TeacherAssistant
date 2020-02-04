using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant {
    public class WindowPageHost : AbstractPageHost<Window> {
        private readonly MainReducer _reducer;

        public override Window BuildContainer<TActivation>(TActivation activation, Control page) {
            var window = new Window {
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
            var isDeactivated = false;

            void OuterDeactivationHandler(object sender, EventArgs args)
            {
                DeactivationHandler(null, null);
            }
            void DeactivationHandler(object sender, EventArgs args)
            {
                if (isDeactivated)
                {
                    return;
                }
                isDeactivated = true;
                this.Pages.Remove(((Window) sender).Uid);
                activation.Deactivate();
                window.Closed -= DeactivationHandler;
                activation.Deactivated -= OuterDeactivationHandler;
            }

            activation.Deactivated += OuterDeactivationHandler;
            window.Closed += DeactivationHandler;
            window.Show();
            return window;
        }

        public override void ClosePage(string id) {
            this.Pages[id].Container.Close();
            this.Pages.Remove(id);
        }

        public WindowPageHost( ModuleActivator activator, MainReducer reducer) : base(activator)
        {
            _reducer = reducer;
        }
    }
}

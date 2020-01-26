using System;
using System.Windows;
using System.Windows.Controls;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant {
    public class WindowPageHost : AbstractPageHost<Window> {
        private readonly IModuleToken _token;

        public override Window BuildContainer<TActivation>(TActivation activation, Control page) {
            var window = new Window {
                Uid = activation.Id,
                Content = page,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            void Handler(object sender, EventArgs args) {
                Pages.Remove(((Window) sender).Uid);
                if (Pages.Count == 0) {
                    _token.Deactivate();
                }

                window.Closed -= Handler;
            }

            window.Closed += Handler;
            window.Show();
            return window;
        }
        public override void ClosePage(string id) {
            Pages[id].Container.Close();
            Pages.Remove(id);
        }

        public WindowPageHost(IModuleToken token, ModuleLoader loader) : base(loader) {
            _token = token;
        }
    }
}
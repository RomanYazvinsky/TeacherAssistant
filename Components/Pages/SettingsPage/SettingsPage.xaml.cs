using System.Reactive.Disposables;
using ReactiveUI;
using Splat;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Pages.SettingsPage.Views;

namespace TeacherAssistant.Pages.SettingsPage {
    public partial class SettingsPage : View<SettingsPageModel> {
        static SettingsPage() {
            Locator.CurrentMutable.Register(() => new AlarmSettingView(), typeof(IViewFor<AlarmSettingsViewModel>));
        }

        public SettingsPage(string id) {
            InitializeComponent();
            InitializeViewModel(id);
            this.WhenActivated(disposable => {
                this.OneWayBind(this.ViewModel, model => model.Alarms, page => page.AlarmControl.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}
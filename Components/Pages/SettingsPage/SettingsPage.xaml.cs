using System.Reactive.Disposables;
using Grace.DependencyInjection;
using ReactiveUI;
using Splat;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Pages.SettingsPage.Views;

namespace TeacherAssistant.Pages.SettingsPage {
    public class SettingsToken : PageModuleToken<SettingsModule> {
        public SettingsToken(string title) :
            base(title) {
        }
    }

    public class SettingsModule : SimpleModule {
        public SettingsModule() : base(typeof(SettingsPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<SettingsPageModel>();
            block.ExportModuleScope<SettingsPage>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel)
                ;
        }
    }

    public class SettingsPageBase : View<SettingsToken, SettingsPageModel> {
    }

    public partial class SettingsPage : SettingsPageBase {
        static SettingsPage() {
            Locator.CurrentMutable.Register(() => new AlarmSettingView(), typeof(IViewFor<AlarmSettingsViewModel>));
        }

        public SettingsPage() {
            InitializeComponent();
            this.WhenActivated(disposable => {
                this.OneWayBind(this.ViewModel, model => model.Alarms, page => page.AlarmControl.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}

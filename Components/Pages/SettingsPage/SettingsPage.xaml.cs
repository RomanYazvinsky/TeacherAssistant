using System.Reactive.Disposables;
using Grace.DependencyInjection;
using ReactiveUI;
using Splat;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.SettingsPage.Views;

namespace TeacherAssistant.Pages.SettingsPage {
    public class SettingsToken : PageModuleToken<SettingsModule> {
        public SettingsToken(string title) :
            base(title) {
        }

        public override PageProperties PageProperties { get; } = new PageProperties {
            InitialHeight = 400,
            InitialWidth = 400
        };
    }

    public class SettingsModule : SimpleModule {
        public SettingsModule() : base(typeof(SettingsPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.DeclareComponent<SettingsPageModel>();
            block.DeclareComponent<SettingsPage>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class SettingsPageBase : View<SettingsToken, SettingsPageModel> {
    }

    public partial class SettingsPage : SettingsPageBase {
        public SettingsPage() {
            InitializeComponent();
            this.WhenActivated(disposable => {
                this.OneWayBind(this.ViewModel, model => model.Alarms, page => page.AlarmControl.ItemsSource)
                    .DisposeWith(disposable);
            });
        }
    }
}

using System.Windows.Controls;
using ReactiveUI;

namespace TeacherAssistant.Pages.SettingsPage.Views {
    public partial class AlarmSettingView : UserControl, IViewFor<AlarmSettingsViewModel> {
        public AlarmSettingView() {
            InitializeComponent();
        }

        object IViewFor.ViewModel {
            get => this.ViewModel;
            set => this.ViewModel = (AlarmSettingsViewModel) value;
        }

        public AlarmSettingsViewModel ViewModel { get; set; }
    }
}
using System;
using System.Reactive.Linq;
using System.Windows;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages {
    public class MainPageView : View<MainWindowPageModel> {
    }

    /// <summary>
    /// Interaction logic for MainWindowPage.xaml
    /// </summary>
    public partial class MainWindowPage : MainPageView {
        public MainWindowPage(string id) {
            InitializeComponent();
            InitializeViewModel(id);
            Storage.Instance.PublishedDataStore
                .DistinctUntilChanged(containers => containers.GetOrDefault<bool>("FullscreenMode"))
                .Select(containers => containers.GetOrDefault<bool>("FullscreenMode")).Subscribe(b => {
                    Menu.Visibility = b ? Visibility.Collapsed : Visibility.Visible;
                });
        }
    }
}
using System.Diagnostics;
using System.Windows;
using Dragablz;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Components.Tabs
{
    public class TB : View<TabManagerModel> {

    }
    /// <summary>
    /// Interaction logic for TabManager.xaml
    /// </summary>
    ///
    public partial class TabManager : TB
    {
        public TabManager(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }

        private void OpenTabOnHover(object sender, object e) {
            TabablzControl.SelectedItem = (sender as DragablzItem).Content;
        }
    }
}

using System;
using System.Reactive.Subjects;
using System.Windows.Controls;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant {
    public class TabPageHost : AbstractPageHost<TabItem>, IDisposable {
        private readonly Subject<TabItem> _tabAdded = new Subject<TabItem>();
        private readonly Subject<TabItem> _tabRemoved = new Subject<TabItem>();


        public TabPageHost(ModuleActivator activator) : base(activator) {
        }

        public override void ClosePage(string id) {
            _tabRemoved.OnNext(Pages[id].Container);
            Pages.Remove(id);
        }

        public override TabItem BuildContainer<TActivation>(TActivation activation, Control control) {
            var textBlock = new TextBlock {Text = activation.Title};
            var tabItem = new TabItem {
                Header = textBlock, Content = control, Uid = activation.Id
            };
            _tabAdded.OnNext(tabItem);
            return tabItem;        }

        public IObservable<TabItem> WhenTabAdded => _tabAdded;
        public IObservable<TabItem> WhenTabClosed => _tabRemoved;


        /*
        private ScrollViewer WrapByScrollView(Control page, PageProperties properties) {
            var viewer = new ScrollViewer {Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto};
            var binding = new Binding("ActualHeight") {Source = viewer};
            page.SetBinding(FrameworkElement.HeightProperty, binding);
            page.MinHeight = properties.MinHeight ?? 0;
            page.MaxHeight = properties.MaxHeight ?? int.MaxValue;
            page.MinWidth = properties.MinWidth ?? 0;
            page.MaxWidth = properties.MaxWidth ?? int.MaxValue;
            return viewer;
        }
*/
        public void Dispose() {
            _tabAdded?.Dispose();
            _tabRemoved?.Dispose();
        }
    }
}
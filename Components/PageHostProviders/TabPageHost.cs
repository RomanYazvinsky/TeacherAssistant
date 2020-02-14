using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using TeacherAssistant.Components;
using TeacherAssistant.Core.Module;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

namespace TeacherAssistant {
    class TabSubscriptionContainer : IDisposable {
        private readonly TabItem _item;
        private readonly IModuleToken _token;
        public event EventHandler<ModuleDestroyEventArgs> Disposed;

        public TabSubscriptionContainer(TabItem item, IModuleToken token) {
            _item = item;
            _token = token;
            _token.Deactivated += DeactivationHandler;
            item.Unloaded += DeactivationHandler;
        }
        private void DeactivationHandler(object sender, EventArgs args) {
            Dispose();
        }

        public void Dispose() {
            Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                _token.Deactivated -= DeactivationHandler;
                _item.Unloaded -= DeactivationHandler;
                Disposed?.Invoke(this, new ModuleDestroyEventArgs(_item, _token));
            }));
        }
    }

    public class TabPageHost : AbstractPageHost<TabItem>, IDisposable {
        private readonly IModuleToken _token;
        private readonly Subject<TabItem> _tabAdded = new Subject<TabItem>();
        private readonly Subject<TabItem> _tabRemoved = new Subject<TabItem>();

        private readonly Dictionary<string, TabSubscriptionContainer> _subscriptionContainers =
            new Dictionary<string, TabSubscriptionContainer>();


        public TabPageHost(IModuleToken token, ModuleActivator activator) : base(activator) {
            _token = token;
        }

        public override string Id => _token.Id;
        public override PageHostType Type => PageHostType.Tab;

        public override void ClosePage(string id) {
            _tabRemoved.OnNext(Pages[id].Container);
            Pages.Remove(id);
        }

        protected override void UnregisterHandlers(IModuleToken token) {
            _tabRemoved.OnNext(Pages[token.Id].Container);
            var tabSubscriptionContainer = _subscriptionContainers[token.Id];
            tabSubscriptionContainer.Disposed -= DestroyModule;
            tabSubscriptionContainer.Dispose();
            _subscriptionContainers.Remove(token.Id);
        }

        public override TabItem BuildContainer<TActivation>(TActivation activation, Control control) {
            var textBlock = new TextBlock {Text = activation.Title};
            var tabItem = new TabItem {
                Header = textBlock,
                Content = control,
                Uid = activation.Id
            };
            var tabSubscriptionContainer = new TabSubscriptionContainer(tabItem, activation);
            _subscriptionContainers.Add(activation.Id, tabSubscriptionContainer);
            tabSubscriptionContainer.Disposed += DestroyModule;
            _tabAdded.OnNext(tabItem);
            return tabItem;
        }

        public IObservable<TabItem> WhenTabAdded => _tabAdded;
        public IObservable<TabItem> WhenTabClosed => _tabRemoved;


        private void DestroyModule(object sender, ModuleDestroyEventArgs args) {
            ClosePage(args.Token.Id);
            args.Token.Deactivate();
        }

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

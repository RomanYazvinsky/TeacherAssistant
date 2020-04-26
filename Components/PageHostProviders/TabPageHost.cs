using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Paging;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant {
    class TabSubscriptionContainer : IDisposable {
        private readonly TabItem _item;
        private readonly IModuleActivation _activation;
        public event EventHandler<ModuleDestroyEventArgs> Disposed;

        public TabSubscriptionContainer(TabItem item, IModuleActivation activation) {
            _item = item;
            _activation = activation;
            _activation.Deactivated += DeactivationHandler;
            item.Unloaded += DeactivationHandler;
        }
        private void DeactivationHandler(object sender, EventArgs args) {
            Dispose();
        }

        public void Dispose() {
            Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                _activation.Deactivated -= DeactivationHandler;
                _item.Unloaded -= DeactivationHandler;
                Disposed?.Invoke(this, new ModuleDestroyEventArgs(_item, _activation));
            }));
        }
    }

    public class TabComponentHost : AbstractComponentHost<TabItem>, IDisposable {
        private readonly IModuleActivation _module;
        private readonly Subject<TabItem> _tabAdded = new Subject<TabItem>();
        private readonly Subject<TabItem> _tabRemoved = new Subject<TabItem>();

        private readonly Dictionary<string, TabSubscriptionContainer> _subscriptionContainers =
            new Dictionary<string, TabSubscriptionContainer>();


        public TabComponentHost(IModuleActivation module, ModuleActivator activator) : base(activator) {
            _module = module;
        }

        public override string Id => _module.Id;
        public override ComponentHostType Type => ComponentHostType.Tab;

        public override void ClosePage(string id) {
            _tabRemoved.OnNext(Pages[id].Container);
            Pages.Remove(id);
        }

        protected override void UnregisterHandlers(IModuleActivation module) {
            _tabRemoved.OnNext(Pages[module.Id].Container);
            var tabSubscriptionContainer = _subscriptionContainers[module.Id];
            tabSubscriptionContainer.Disposed -= DestroyModule;
            tabSubscriptionContainer.Dispose();
            _subscriptionContainers.Remove(module.Id);
        }

        public override TabItem BuildContainer<TActivation>(TActivation activation, Control control) {
            var textBlock = new TextBlock {Text = activation.GetToken().Title};
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
            ClosePage(args.ModuleActivation.Id);
            args.ModuleActivation.Deactivate();
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

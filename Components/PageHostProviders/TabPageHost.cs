using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Containers;
using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class TabPageHost : AbstractPageHost<TabItem> {
        public TabPageHost(string providerId, PageService pageService) : base(pageService) {
            this.ProviderId = providerId;
        }

        public override string ProviderId { get; }

        protected override TabItem PlaceInContainer(string id, Control page, IPageProperties properties) {
            var textBlock = new TextBlock {Text = properties.Header};
            var tabItem = new TabItem {
                Header = textBlock, Content = page, Uid = id
            };
            return tabItem;
        }

        public override event EventHandler<TabItem> PageAdded;
        public override event EventHandler<TabItem> PageClosed;
        public override event EventHandler<TabItem> PageDetached;
        public override event EventHandler<TabItem> PageAttached;
        public override event EventHandler<PageChanges> PageChanged;


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


        public override void ClosePage(string id) {
            if (_pages.Count < 2) {
                return;
            }

            PageClosed?.Invoke(this, _pages[id].Container);
            _pages.Remove(id);
        }

        public override void ChangePage<TItem>(string id, PageProperties<TItem> config) {
            var page = _pages[id];
            var newPage = BuildPageInfo(id, config.Type, config, page);
            _pages[id] = newPage;
            PageChanged?.Invoke(this, new PageChanges(id, page.Container, newPage.Container));
        }

        public override void GoBack(string id) {
            var page = _pages[id];
            if (page.Previous == null) {
                return;
            }

            var oldPage = page.Previous;
            _pages[id] = oldPage;
            PageChanged?.Invoke(this, new PageChanges(id, page.Container, oldPage.Container));
        }

        public override void GoForward(string id) {
            var page = _pages[id];
            if (page.Next == null) {
                return;
            }

            var oldPage = page.Next;
            _pages[id] = oldPage;
            PageChanged?.Invoke(this, new PageChanges(id, page.Container, oldPage.Container));
        }

        public override void Refresh(string id) {
        }

        public override PageInfo<TabItem> Detach(string id) {
            var result = base.Detach(id);
            var iter = result;
            while (iter.Previous != null) {
                iter = iter.Previous;
                BindingOperations.ClearAllBindings(iter.Page);
            }

            iter = result;

            while (iter.Next != null) {
                iter = iter.Next;
                BindingOperations.ClearAllBindings(iter.Page);
            }

            BindingOperations.ClearAllBindings(result.Page);
            return result;
        }

        public override string Attach<T>(PageInfo<T> info) {
            var pageInfo = WrapToContainer(info);
            _pages.Add(pageInfo.Id, pageInfo);
            PageAttached?.Invoke(this, pageInfo.Container);
            return pageInfo.Id;
        }

        protected override void CallPageAdded(TabItem container) {
            PageAdded?.Invoke(this, container);
        }

        protected override void CallPageClosed(TabItem container) {
            PageClosed?.Invoke(this, container);
        }

        protected override void CallPageDetached(TabItem container) {
            PageDetached?.Invoke(this, container);
        }
    }
}
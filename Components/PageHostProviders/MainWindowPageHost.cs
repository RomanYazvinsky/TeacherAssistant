using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Containers;
using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class MainWindowPageHost : AbstractPageHost<Window> {
        public MainWindowPageHost(string providerId, PageService pageService) : base(pageService) {
            this.ProviderId = providerId;
        }

        protected override Window PlaceInContainer(string id, Control page, IPageProperties properties) {
            var window = new Window() {
                Uid = id,
                Content = page,
                MaxHeight = properties.MaxHeight ?? double.PositiveInfinity,
                MinHeight = properties.MinHeight ?? 0,
                MaxWidth = properties.MaxWidth ?? double.PositiveInfinity,
                MinWidth = properties.MinWidth ?? 0,
                Width = properties.DefaultWidth ?? properties.MinWidth ?? 800,
                Height = properties.DefaultHeight ?? properties.MinHeight ?? 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var disposable = Storage.Instance.PublishedDataStore
                .DistinctUntilChanged(containers => containers.GetOrDefault<bool>("FullscreenMode"))
                .Select(containers => containers.GetOrDefault<bool>("FullscreenMode")).Subscribe(b => {
                    window.WindowStyle = b ? WindowStyle.None : WindowStyle.SingleBorderWindow;
                });
            window.KeyDown += (sender, args) => {
                if (args.Key == Key.F11) {
                    new Storage.ToggleFullscreen().Dispatch();
                }
            };
            window.Closing += (sender, args) => {
                disposable.Dispose();
                //TODO fix cleaning
                /*pageService.ClosePage(window.Uid);
                pageService.RemovePageHost((sender as Window)?.Uid);*/
            };
            return window;
        }

        public override event EventHandler<Window> PageAdded;

        public override event EventHandler<Window> PageClosed;
        public override event EventHandler<Window> PageDetached;
        public override event EventHandler<Window> PageAttached;
        public override event EventHandler<PageChanges> PageChanged;
        public override string ProviderId { get; }

        public override void ClosePage(string id) {
            var pageInfo = _pages[id];
            var window = pageInfo.Container;
            window?.Close();
            _pages.Remove(id);
            CallPageClosed(window);
        }

        public override void ChangePage<TPage>(string id, PageProperties<TPage> config) {
        }

        public override void GoBack(string id) {
        }

        public override void GoForward(string id) {
        }

        public override void Refresh(string id) {
        }

        public override string Attach<T1>(PageInfo<T1> info) {
            var pageInfo = WrapToContainer(info);
            _pages.Add(pageInfo.Id, pageInfo);
            PageAttached?.Invoke(this, pageInfo.Container);
            return pageInfo.Id;
        }

        protected override void CallPageAdded(Window container) {
            PageAdded?.Invoke(this, container);
        }

        protected override void CallPageClosed(Window container) {
            PageClosed?.Invoke(this, container);
        }

        protected override void CallPageDetached(Window container) {
            PageDetached?.Invoke(this, container);
        }
    }
}
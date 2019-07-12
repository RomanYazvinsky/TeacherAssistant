using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Ninject;
using Ninject.Parameters;
using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class PageInfo<T> where T : Control {
        public PageInfo(PageInfo<T> info, T container, Control page, PageProperties properties) :
            this(info.Id, container, page, properties) {
            info.Next = this;
            this.Previous = info;
        }

        public PageInfo(string id, T container, Control page, PageProperties properties) {
            this.Id = id;
            this.Properties = properties;
            this.Page = page;
            this.Container = container;
        }

        public string Id { get; set; }
        public Control Page { get; set; }
        public T Container { get; set; }
        public PageInfo<T> Previous { get; }
        public PageInfo<T> Next { get; private set; }
        public PageProperties Properties { get; set; }
    }

    public static class IdGenerator {
        private const string IdValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random Random = new Random();

        public static string GenerateId(int length) {
            return new string
            (
                Enumerable.Repeat(IdValues, length)
                    .Select(s => s[Random.Next(s.Length)])
                    .ToArray()
            );
        }
    }

    public abstract class AbstractPageHost<T> : IPageContainerProvider<T> where T : Control {
        protected Dictionary<string, PageInfo<T>> _pages = new Dictionary<string, PageInfo<T>>();
        protected readonly PageService pageService;

        protected AbstractPageHost(PageService pageService) {
            this.pageService = pageService;
        }

        protected abstract T PlaceInContainer(string id, Control page, PageProperties properties);

        protected PageInfo<T> BuildPageInfo(string id,
            Type componentType,
            PageProperties properties,
            PageInfo<T> prevPage = null
        ) {
            var page = (UserControl) Injector.Instance.Kernel.Get
            (
                componentType,
                new ConstructorArgument("id", id),
                new ConstructorArgument("pageProvider", this)
            );

            return prevPage == null
                ? new PageInfo<T>(id, PlaceInContainer(id, page, properties), page, properties)
                : new PageInfo<T>(prevPage, PlaceInContainer(id, page, properties), page, properties);
        }

        public abstract event EventHandler<T> PageAdded;
        public abstract event EventHandler<T> PageClosed;
        public abstract event EventHandler<T> PageDetached;
        public abstract event EventHandler<T> PageAttached;
        public abstract event EventHandler<PageChanges> PageChanged;

        public abstract string ProviderId { get; }

        public string AddPage(PageProperties config) {
            var id = IdGenerator.GenerateId(10);
            var pageInfo = BuildPageInfo(id, config.PageType, config);
            _pages.Add(id, pageInfo);
            CallPageAdded(pageInfo.Container);
            return id;
        }

        public abstract void ClosePage(string id);
        public abstract void ChangePage(string id, PageProperties config);
        public abstract void GoBack(string id);
        public abstract void GoForward(string id);
        public abstract void Refresh(string id);

        public T GetCurrentControl(string id) {
            return _pages[id].Container;
        }

        public abstract string Attach<T1>(PageInfo<T1> info) where T1 : Control;

        public virtual PageInfo<T> Detach(string id) {
            PageInfo<T> detached = _pages[id];
            _pages.Remove(id);
            CallPageDetached(detached.Container);
            return detached;
        }

        protected PageInfo<T> WrapToContainer<TV>(PageInfo<TV> pageInfo) where TV : Control {
            var currentOldPage = pageInfo;
            while (pageInfo.Previous != null) {
                // find the first page
                currentOldPage = pageInfo.Previous;
            }

            var currentNew = new PageInfo<T>
            (
                currentOldPage.Id,
                PlaceInContainer(currentOldPage.Id, currentOldPage.Page, currentOldPage.Properties),
                currentOldPage.Page,
                currentOldPage.Properties
            );
            var result = currentNew;
            while (currentOldPage.Next != null) {
                // restore the sequence of pages
                currentOldPage = currentOldPage.Next;
                currentNew = new PageInfo<T>
                (
                    currentNew,
                    PlaceInContainer(currentOldPage.Id, currentOldPage.Page, currentOldPage.Properties),
                    currentOldPage.Page,
                    currentOldPage.Properties
                );
                if (currentOldPage.Id.Equals(pageInfo.Id)) {
                    result = currentNew; // find currently active page
                }
            }

            return result;
        }

        protected abstract void CallPageAdded(T container);
        protected abstract void CallPageClosed(T container);
        protected abstract void CallPageDetached(T container);

        public void Dispose() {
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using TeacherAssistant.Components;
using TeacherAssistant.State;

namespace TeacherAssistant {
    public class PageService : IDisposable {
        private readonly Dictionary<string, IPageProvider> _pageProviders =
            new Dictionary<string, IPageProvider>();

        private readonly Dictionary<string, IPageProvider> _pageHosts =
            new Dictionary<string, IPageProvider>();

        public void RegisterPageHost(IPageProvider pageContainer) {
            _pageProviders.Add(pageContainer.ProviderId, pageContainer);
            StoreManager.Add("PageProviders", pageContainer.ProviderId);
        }

        public void RemovePageHost(string pageContainerId) {
            if (!_pageProviders.ContainsKey(pageContainerId)) {
                return;
            }
            var pageContainerProvider = _pageProviders[pageContainerId];
            pageContainerProvider.Dispose();
            // remove from pageHosts
            _pageProviders.Remove(pageContainerId);
            StoreManager.Remove("PageProviders", pageContainerId);
        }

        public void ChangePage<T>(string pageId, PageProperties<T> config) {
            _pageHosts[pageId].ChangePage(pageId, config);
        }

        public string OpenPage<T>(string pageHostProviderId, PageProperties<T> config){
            var pageHost = _pageProviders[pageHostProviderId];
            var pageId = pageHost.AddPage(config);
            _pageHosts.Add(pageId, pageHost);
            StoreManager.Publish(pageHost, pageId, "Provider");
            return pageId;
        }

        public string OpenPage<T>(PageProperties<T> config, string calleeId) {
            var pageHost = _pageHosts[calleeId];
            var pageId = pageHost.AddPage(config);
            _pageHosts.Add(pageId, pageHost);
            StoreManager.Publish(pageHost, pageId, "Provider");
            return pageId;
        }

        public string MovePage<T>(string to, string pageId) where T : Control {
            var source = _pageHosts[pageId];
            var target = _pageProviders[to];
            var pageInfo = ((IPageContainerProvider<T>) source).Detach(pageId);
            _pageHosts.Remove(pageId);
            _pageHosts.Add(pageId, target);
            StoreManager.Publish(target, pageId, "Provider");
            return target.Attach(pageInfo);
        }

        public string GetProviderId(string id) {
            return _pageHosts[id].ProviderId;
        }

        public void ClosePage(string id) {
            if (!_pageHosts.ContainsKey(id)) {
                return;
            }
            _pageHosts[id].ClosePage(id);
            _pageHosts.Remove(id);
        }

        public IPageContainerProvider<T> GetProvider<T>(string id) where T : Control {
            return (IPageContainerProvider<T>) _pageProviders[id];
        }

        public void Dispose() {
        }
    }
}
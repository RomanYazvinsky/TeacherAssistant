using System;
using System.Windows.Controls;
using Containers;
using TeacherAssistant.State;

namespace TeacherAssistant.Components {
    public class PageChanges {
        public string Id { get; }
        public Control From { get; }
        public Control To { get; }

        public PageChanges(string id, Control from, Control to) {
            this.Id = id;
            this.From = from;
            this.To = to;
        }
    }

    public interface IPageProvider : IDisposable {
        string ProviderId { get; }
        string AddPage(PageProperties config);
        void ClosePage(string id);
        void ChangePage(string id, PageProperties config);
        void GoBack(string id);
        void GoForward(string id);
        void Refresh(string id);
        
    
        string Attach<T>(PageInfo<T> info) where T : Control; 
    }

    public interface IPageContainerProvider<T> : IPageProvider where T : Control {
        event EventHandler<T> PageAdded;
        event EventHandler<T> PageClosed;
        event EventHandler<T> PageDetached;
        event EventHandler<T> PageAttached;
        event EventHandler<PageChanges> PageChanged;
        T GetCurrentControl(string id);

        PageInfo<T> Detach(string id);
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl
{
    public class AbstractModel : INotifyPropertyChanged, IDisposable
    {
        private List<IDisposable> _internalSubscriptions;
        protected string _id;
        public event PropertyChangedEventHandler PropertyChanged;

        protected AbstractModel(string id)
        {
            _id = id;
            _internalSubscriptions = new List<IDisposable>();
        }

        protected void S<T>(string id, Action<T> action)
        {
            _internalSubscriptions.Add(new Subscriber<T>(id, action).SubscribeOnChanges());
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _internalSubscriptions.ForEach(disposable => disposable.Dispose());
        }
    }
}
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Redux;
using TeacherAssistant.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl
{
    public class AbstractModel : INotifyPropertyChanged, IDisposable
    {
        protected string _id;
        public event PropertyChangedEventHandler PropertyChanged;
        protected IStore<ImmutableDictionary<string, DataContainer>> _store;
        protected Subject<bool> _disposer;

        protected AbstractModel(string id = null)
        {
            _disposer = new Subject<bool>();
            _id = id;
            _store = DataExchangeManagement.GetInstance().PublishedDataStore;
        }

        protected void SimpleSubscribe<T>(string id, Action<T> action)
        {
            _store.DistinctUntilChanged(containers => containers.GetOrDefault<T>(id)).TakeUntil(_disposer)
                .Subscribe(containers => action(containers.GetOrDefault<T>(id)));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _disposer.Next();
            _disposer.Dispose();
        }
    }
}
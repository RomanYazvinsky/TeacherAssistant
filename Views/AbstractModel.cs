using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Redux;
using TeacherAssistant.Annotations;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl
{
    public abstract class AbstractModel : INotifyPropertyChanged, IDisposable
    {
        protected string _id;
        public event PropertyChangedEventHandler PropertyChanged;
        protected IStore<ImmutableDictionary<string, DataContainer>> _store;
        protected Subject<bool> _disposer;

        protected AbstractModel()
        {
            _disposer = new Subject<bool>();
            _store = DataExchangeManagement.GetInstance().PublishedDataStore;
        }

        public abstract Task Init(string id);

        protected void SimpleSubscribe<T>(string id, Action<T> action)
        {
            _store.DistinctUntilChanged(containers => containers.GetOrDefault<T>(id)).TakeUntil(_disposer)
                .Subscribe(containers => action(containers.GetOrDefault<T>(id)));
        }
        protected void SimpleSubscribeCollection<T>(string id, Action<ICollection<T>> action)
        {
            _store.DistinctUntilChanged(containers => containers.GetOrDefault<ICollection<T>>(id)).TakeUntil(_disposer)
                .Subscribe(containers => action(containers.GetOrDefault<ICollection<T>>(id)));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _disposer.OnNext(true);
            _disposer.Dispose();
        }
    }
}
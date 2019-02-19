using System;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace TeacherAssistant.State
{
    public class Subscriber<T>
    {
        private string _key;
        private Action<T> _action;

        public void Run(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state)
        {

            _action(Publisher.Get<T>(state, _key));
        }

        public T Get(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state)
        {
            return Publisher.Get<T>(state, _key);
        }


        public IDisposable SubscribeOnChanges()
        {
            return DataExchangeManagement.GetInstance().PublishedDataStore.DistinctUntilChanged(Get).Subscribe(Run);
        }

        public Subscriber(string key, Action<T> action)
        {
            _key = key;
            _action = action;
        }
    }
}
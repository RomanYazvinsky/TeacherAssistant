using System;
using System.Collections.Immutable;
using System.Reactive.Linq;

namespace TeacherAssistant.State
{
    public class Subscriber<T> : IDisposable
    {
        private string _key;
        private Action<T> _action;
        private bool _active = true;

        public void Run(ImmutableDictionary<string, DataContainer> state)
        {

            _action(Publisher.Get<T>(state, _key));
        }

        public T Get(ImmutableDictionary<string, DataContainer> state)
        {
            return Publisher.Get<T>(state, _key);
        }


        public IDisposable SubscribeOnChanges()
        {
            return DataExchangeManagement.GetInstance().PublishedDataStore.DistinctUntilChanged(Get).TakeWhile(containers => _active).Subscribe(Run);
        }

        public Subscriber(string key, Action<T> action)
        {
            _key = key;
            _action = action;
        }

        public void Dispose()
        {
            _active = false;
        }
    }
}
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Redux;

namespace Views
{
    public class StoreSelector<T>
    {
        private string _key;
        private Action<T> _action;

        public void Run(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state) 
        {

            _action(Get<T>(state, _key));
        }

        public T Get(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state)
        {
            return Get<T>(state, _key);
        }

        public static V Get<V>(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state, string key)
        {
            if (state.ContainsKey(key))
            {
                return state[key].GetData<V>();
            }

            return default(V);
        }
        public StoreSelector(string key, Action<T> action)
        {
            _key = key;
            _action = action;
        }
    }
}
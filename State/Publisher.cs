using System.Collections.Immutable;

namespace TeacherAssistant.State
{
    public class Publisher
    {
        private Publisher()
        {
        }

        public static void Publish<T>(string id, T data)
        {
            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish
            {
                Id = id,
                Data = data
            });
        }

        public static V Get<V>(ImmutableDictionary<string, DataContainer> state, string key)
        {
            return state.ContainsKey(key) ? state[key].GetData<V>() : default(V);
        }

        public static V Get<V>(string key)
        {
            var state = DataExchangeManagement.GetInstance().PublishedDataStore.GetState();
            return state.ContainsKey(key) ? state[key].GetData<V>() : default(V);
        }
    }
}
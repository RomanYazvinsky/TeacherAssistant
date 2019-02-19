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

        public static V Get<V>(ImmutableDictionary<string, DataExchangeManagement.DataContainer> state, string key)
        {
            if (state.ContainsKey(key))
            {
                return state[key].GetData<V>();
            }

            return default(V);
        }
    }
}
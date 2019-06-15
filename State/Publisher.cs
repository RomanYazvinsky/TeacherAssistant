using System.Collections.Generic;
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

        public static void Add<T>(string id, T data)
        {
            var collection = Get<ICollection<T>>(id);
            collection = collection == null ? new List<T>() : new List<T>(collection);
            collection.Add(data);
            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish
            {
                Id = id,
                Data = collection
            });
        }

        public static void Remove<T>(string id, T data)
        {
            var collection = Get<ICollection<T>>(id);
            if (collection == null)
            {
                collection = new List<T>();
            }
            else
            {
                collection = new List<T>(collection);
                if (collection.Contains(data))
                {
                    collection.Remove(data);
                }
            }

            DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish
            {
                Id = id,
                Data = collection
            });
        }

        public static V Get<V>(ImmutableDictionary<string, DataContainer> state, string key)
        {
            return state.GetOrDefault<V>(key);
        }

        public static V Get<V>(string key)
        {
            var state = DataExchangeManagement.GetInstance().PublishedDataStore.GetState();
            return Get<V>(state, key);
        }
    }
}
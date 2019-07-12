using System.Collections.Generic;
using System.Collections.Immutable;

namespace TeacherAssistant.State {
    public class StoreManager {
        private StoreManager() {
        }

        public static void Publish<T>(string id, T data) {
            new Storage.PublishAction {
                Id = id,
                Data = data
            }.Dispatch();
        }

        public static void Publish<T>(T data, params string[] id) {
            new Storage.PublishAction {
                Id = string.Join(".", id),
                Data = data
            }.Dispatch();
        }

        public static void Add<T>(string id, T data) {
            var collection = Get<ICollection<T>>(id);
            collection = collection == null ? new List<T>() : new List<T>(collection);
            collection.Add(data);
            new Storage.PublishAction {
                Id = id,
                Data = collection
            }.Dispatch();
        }

        public static void Remove<T>(string id, T data) {
            var collection = Get<ICollection<T>>(id);
            if (collection == null) {
                collection = new List<T>();
            }
            else {
                collection = new List<T>(collection);
                if (collection.Contains(data)) {
                    collection.Remove(data);
                }
            }

            new Storage.PublishAction {
                Id = id,
                Data = collection
            }.Dispatch();
        }

        public static V Get<V>(ImmutableDictionary<string, DataContainer> state, params string[] key) {
            return state.GetOrDefault<V>(string.Join(".", key));
        }

        public static V Get<V>(params string[] key) {
            var state = Storage.Instance.PublishedDataStore.GetState();
            return Get<V>(state, key);
        }
    }
}
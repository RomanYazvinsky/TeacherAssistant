using System.Collections.Generic;
using System.Collections.Immutable;
using Containers;
using Redux;

namespace TeacherAssistant.State {
    public class Storage {
        private static Storage _instance;

        private Storage() {
            this.PublishedDataStore =
                new Store<ImmutableDictionary<string, DataContainer>>
                (
                    GeneralReducer.ActionHandler,
                    new Dictionary<string, DataContainer> {
                        {"FullscreenMode", new DataContainer(false)},
                        {"DragData", new DataContainer(null)}
                    }.ToImmutableDictionary()
                );
        }

        public IStore<ImmutableDictionary<string, DataContainer>> PublishedDataStore { get; }

        public static Storage Instance => _instance ?? (_instance = new Storage());

        public abstract class AbstractAction : IAction {
            public void Dispatch() {
                Instance.PublishedDataStore.Dispatch(this);
            }
        }

        public class PublishAction : AbstractAction {
            public string Id { get; set; }
            public object Data { get; set; }
        }

        public class ToggleFullscreen : AbstractAction {
            public bool? Value { get; set; }
        }

        public class CleanupAction : AbstractAction {
            public CleanupAction(string id) {
                this.Id = id;
            }

            public string Id { get; }
        }

        public class DragStart : AbstractAction {
            public DragStart(DragData dragData) {
                this.DragData = dragData;
            }

            public DragData DragData { get; }
        }
    }
}
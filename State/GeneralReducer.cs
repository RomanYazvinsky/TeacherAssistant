using System.Collections.Immutable;
using System.Linq;
using Containers;
using Redux;

namespace TeacherAssistant.State {
    public class GeneralReducer {
        public static ImmutableDictionary<string, DataContainer> ActionHandler(
            ImmutableDictionary<string, DataContainer> state, IAction action) {
            switch (action) {
                case Storage.PublishAction publishAction: {
                    if (publishAction.Id == null)
                        return state;

                    if (!(publishAction.Data is DataContainer)) {
                        publishAction.Data = new DataContainer(publishAction.Data);
                    }

                    if (!state.TryGetKey(publishAction.Id, out _))
                        return state.Add
                        (
                            publishAction.Id,
                            (DataContainer) publishAction.Data
                        );

                    var newState = state.SetItem(publishAction.Id, (DataContainer) publishAction.Data);

                    return newState;
                }


                case Storage.ToggleFullscreen fullscreen: {
                    var currentState = StoreManager.Get<bool>("FullscreenMode");
                    currentState = fullscreen.Value ?? !currentState;
                    return state.SetItem("FullscreenMode", new DataContainer(currentState));
                }

                case Storage.CleanupAction cleanup: {
                    return state.RemoveRange(state.Keys.Where(key => key.StartsWith(cleanup.Id + ".")));
                }
                case Storage.DragStart dragStart: {
                    return state.SetItem("DragData", new DataContainer(dragStart.DragData));
                }
                default: {
                    return state;
                }
            }
        }
    }
}
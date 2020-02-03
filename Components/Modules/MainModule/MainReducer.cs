using System.Collections.Immutable;
using Redux;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Modules.MainModule {
    public class MainReducer : AbstractReducer<MainState> {
        public MainReducer(MainModuleToken id, Storage storage) : base(id, storage) {
        }

        public override ImmutableDictionary<string, object> Reduce(ImmutableDictionary<string, object> state,
            IAction action) {
            if (action is SetFullscreenModeAction sfma) {
                if (sfma.Fullscreen == null) {
                    var currentValue = SelectCurrentValue(mainState => mainState.FullscreenMode, state);
                    return Set(mainState => mainState.FullscreenMode, state, !currentValue);
                }
                return Set(mainState => mainState.FullscreenMode, state, sfma.Fullscreen);
            }

            return state;
        }
    }
}
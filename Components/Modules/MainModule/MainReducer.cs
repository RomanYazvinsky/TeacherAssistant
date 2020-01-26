using System.Collections.Immutable;
using Redux;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Modules.MainModule {
    public class MainReducer : AbstractReducer<MainState> {
        public MainReducer(IModuleToken id, Storage storage) : base(id, storage) {
        }

        public override ImmutableDictionary<string, object> Reduce(ImmutableDictionary<string, object> state,
            IAction action) {
            if (action is SetFullscreenModeAction sfma) {
                return Set(mainState => mainState.FullscreenMode, state, sfma.Fullscreen);
            }

            return state;
        }
    }
}
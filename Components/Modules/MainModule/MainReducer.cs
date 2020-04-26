using System.Collections.Immutable;
using Redux;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Modules.MainModule
{
    public class MainReducer : AbstractReducer<MainState>
    {
        public MainReducer(IModuleActivation activation, Storage storage) : base(activation, storage)
        {
        }

        public override ImmutableDictionary<string, object> Reduce(ImmutableDictionary<string, object> state,
            IAction action)
        {
            if (!(action is SetFullscreenModeAction sfma))
            {
                return state;
            }

            if (sfma.Fullscreen != null)
            {
                return Set(mainState => mainState.FullscreenMode, state, sfma.Fullscreen);
            }

            var currentValue = SelectCurrentValue(mainState => mainState.FullscreenMode, state);
            return Set(mainState => mainState.FullscreenMode, state, !currentValue);
        }

        protected override void CleanupModuleData()
        {
            // Not needed - store is destroyed
        }
    }
}
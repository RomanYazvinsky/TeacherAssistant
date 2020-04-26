using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using Redux;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages.PageController {
    using GlobalState = ImmutableDictionary<string, object>;

    public class PageControllerState {
        public ImmutableDictionary<string, List<ButtonConfig>> Controls { get; } =
            new Dictionary<string, List<ButtonConfig>>().ToImmutableDictionary();

        public IModuleActivation SelectedPage { get; } = null;
    }

    public class PageControllerReducer : AbstractReducer<PageControllerState> {
        public PageControllerReducer(IModuleActivation activation, Storage storage)
            : base(activation, storage) {
        }

        public override GlobalState Reduce(GlobalState state, IAction action) {
            if (action is SetControlsAction setControlsAction) {
                return Set(controllerState => controllerState.Controls, state, setControlsAction.Controls);
            }

            return state;
        }
    }

    public class PageControllerEffects : SimpleEffects<GlobalState> {
        public PageControllerEffects(PageControllerReducer reducer, SimpleEffectsMiddleware<GlobalState> effects, Storage storage) :
            base(effects, storage) {
            CreateEffect(actions =>
                actions
                    .OfType<RegisterControlsAction>()
                    .WithLatestFrom(reducer.Select(state => state.Controls), LambdaHelper.ToTuple)
                    .Select(tuple => {
                        var (action, currentConfig) = tuple;

                        var config = currentConfig;

                        void Handle(object _, object __) {
                            config.Remove(action.ModuleActivation.Id);
                            action.ModuleActivation.Deactivated -= Handle;
                        }

                        action.ModuleActivation.Deactivated += Handle;
                        currentConfig = currentConfig.SetItem(action.ModuleActivation.Id, action.Configs);
                        return reducer.SetActionId(new SetControlsAction(currentConfig));
                    })
            );
        }
    }

    public class RegisterControlsAction : ModuleScopeAction {
        public List<ButtonConfig> Configs { get; }
        public IModuleActivation ModuleActivation { get; }

        public RegisterControlsAction(IModuleActivation moduleActivation, List<ButtonConfig> configs) {
            this.ModuleActivation = moduleActivation;
            this.Configs = configs;
        }
    }

    public class SetControlsAction : ModuleScopeAction {
        public ImmutableDictionary<string, List<ButtonConfig>> Controls { get; }

        public SetControlsAction(ImmutableDictionary<string, List<ButtonConfig>> controls) {
            this.Controls = controls;
        }
    }
}

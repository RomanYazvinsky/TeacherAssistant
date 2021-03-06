using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Linq;
using Containers;
using Redux;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages {
    using GlobalState = ImmutableDictionary<string, object>;

    public class PageControllerState {
        public ImmutableDictionary<string, List<ButtonConfig>> Controls { get; } =
            new Dictionary<string, List<ButtonConfig>>().ToImmutableDictionary();

        public IModuleToken SelectedPage { get; } = null;
    }

    public class PageControllerReducer : AbstractReducer<PageControllerState> {
        public PageControllerReducer(PageControllerToken token, Storage storage)
            : base(token, storage) {
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
                            config.Remove(action.Token.Id);
                            action.Token.Deactivated -= Handle;
                        }

                        action.Token.Deactivated += Handle;
                        currentConfig = currentConfig.SetItem(action.Token.Id, action.Configs);
                        return reducer.SetActionId(new SetControlsAction(currentConfig));
                    })
            );
        }
    }

    public class RegisterControlsAction : ModuleScopeAction {
        public List<ButtonConfig> Configs { get; }
        public IModuleToken Token { get; }

        public RegisterControlsAction(IModuleToken token, List<ButtonConfig> configs) {
            this.Token = token;
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

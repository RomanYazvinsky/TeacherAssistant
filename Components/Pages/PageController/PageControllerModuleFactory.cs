using System.Collections.Generic;
using System.Collections.Immutable;
using Containers;
using Redux;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Pages {

    public class PageControllerState {
        public List<ButtonConfig> Controls { get; } = new List<ButtonConfig>();
    }

    public class PageControllerReducer : AbstractReducer<PageControllerState> {
        public PageControllerReducer(IModuleToken token, Storage storage) : base(token, storage) {
        }

        public override ImmutableDictionary<string, object> Reduce(ImmutableDictionary<string, object> state, IAction action) {
            return state;
        }
    }
}
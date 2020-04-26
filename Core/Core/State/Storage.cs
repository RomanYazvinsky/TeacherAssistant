using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using Redux;
using TeacherAssistant.Core.Effects;
using TeacherAssistant.Core.Reducers;
using TeacherAssistant.State;

namespace TeacherAssistant.Core.State
{
    using GlobalState = ImmutableDictionary<string, object>;

    public class Storage
    {
        private readonly Dictionary<string, IReducer> _reducers = new Dictionary<string, IReducer>();
        public Store<GlobalState> Store { get; }

        public Storage(SimpleEffectsMiddleware<GlobalState> effectsMiddleware)
        {
            this.Store = new Store<GlobalState>(
                (state, action) =>
                {
                    if (action is CleanupAction cleanupAction)
                    {
                        return state.RemoveRange(state.Keys.Where(s => s.StartsWith(cleanupAction.Id)));
                    }

                    if (action is SetValueAction setValueAction)
                    {
                        return state.SetItem(setValueAction.PropertyName, setValueAction.Value);
                    }

                    if (action is ModuleScopeAction moduleScopeAction && moduleScopeAction.Id != null)
                    {
                        var immutableDictionary = _reducers.Aggregate(state,
                            (tempState, pair) => moduleScopeAction.Id.Equals(pair.Key)
                                ? pair.Value.Reduce(tempState, action)
                                : tempState);
                        return immutableDictionary;
                    }

                    if (action is SetupStateAction setupState)
                    {
                        return setupState.State;
                    }

                    return _reducers.Aggregate(state, (objects, pair) => pair.Value.Reduce(state, action));
                },
                new Dictionary<string, object>
                {
                }.ToImmutableDictionary(),
                effectsMiddleware.RegisterMiddleware());
        }

        public Func<IObservable<TMember>> CreateSelector<TMember>(string key)
        {
            return () => this.Store
                .DistinctUntilChanged(c => c.GetOrDefault(key))
                .Select(c => c.GetOrDefault(key)).Cast<TMember>();
        }


        public void RegisterReducer(string id, IReducer reducer)
        {
            _reducers.Add(id, reducer);
        }

        public void UnregisterReducer(string id)
        {
            _reducers.Remove(id);
            this.Store.Dispatch(new CleanupAction(id));
        }

        private class CleanupAction : ModuleScopeAction
        {
            public CleanupAction(string id)
            {
                this.Id = id;
            }
        }

        public class SetupStateAction : IAction
        {
            public SetupStateAction(GlobalState state)
            {
                this.State = state;
            }

            public GlobalState State { get; }
        }

        public class SetValueAction : ModuleScopeAction
        {
            public SetValueAction(string propertyName, object value)
            {
                this.PropertyName = propertyName;
                this.Value = value;
            }

            public string PropertyName { get; }
            public object Value { get; }
        }
    }

    public class ModuleScopeAction : IAction
    {
        public string Id { get; internal set; }
    }
}
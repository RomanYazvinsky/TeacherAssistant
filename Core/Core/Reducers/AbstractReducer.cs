using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Redux;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Core.Reducers {
    using GlobalState = ImmutableDictionary<string, object>;

    public interface IReducer {
        GlobalState Reduce(GlobalState state, IAction action);
    }

    public abstract class AbstractReducer<TState> : IReducer, IDisposable where TState : new() {
        private readonly string _id;
        protected readonly Storage Storage;
        private readonly Subject<object> _disposeSubject = new Subject<object>();

        protected AbstractReducer(IModuleToken token, Storage storage) {
            _id = token.Id;
            Storage = storage;
            Storage.RegisterReducer(_id, this);
            InitializeState();
        }


        protected GlobalState Set<TMember>(Expression<Func<TState, TMember>> expression,
            ImmutableDictionary<string, object> state,
            TMember value) {
            return state.SetItem(GetKey(expression), value);
        }

        protected GlobalState Set<TMember>(Expression<Func<TState, string>> expression,
            GlobalState state,
            TMember value) {
            return state.SetItem(GetKey(expression), value);
        }

        protected string GetKey<TMember>(Expression<Func<TState, TMember>> expression) {
            return _id + (expression.Body is MemberExpression member ? member.Member.Name : "");
        }

        public IObservable<TMember> Select<TMember>(Expression<Func<TState, TMember>> expression) {
            return Storage.CreateSelector<TMember>(GetKey(expression))().TakeUntil(_disposeSubject);
        }

        public TMember SelectCurrentValue<TMember>(Expression<Func<TState, TMember>> expression, GlobalState state) {
            return (TMember) state[GetKey(expression)];
        }

        public TAction SetActionId<TAction>(TAction action) where TAction: ModuleScopeAction {
            action.Id = _id;
            return action;
        }
        public void Dispatch(IAction action) {
            if (action is ModuleScopeAction moduleScopeAction) {
                moduleScopeAction.Id = _id;
            }

            Storage.Store.Dispatch(action);
        }

        public void DispatchSetValueAction<TMember>(Expression<Func<TState, TMember>> expression, TMember value) {
            Dispatch(new Storage.SetValueAction(GetKey(expression), value));
        }

        private void InitializeState() {
            var localState = new TState();
            var state = Storage.Store.GetState();
            state = localState.GetType().GetProperties().Aggregate(state,
                (current, propertyInfo) => current.SetItem(propertyInfo.Name, propertyInfo.GetValue(localState)));
            Dispatch(new Storage.SetupStateAction(state));
        }

        public void Dispose() {
            Storage.UnregisterReducer(_id);
            _disposeSubject.OnNext(1);
            _disposeSubject.OnCompleted();
        }

        public abstract GlobalState Reduce(GlobalState state,
            IAction action);
    }
}
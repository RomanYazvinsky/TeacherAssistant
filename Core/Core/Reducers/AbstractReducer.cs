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
        private readonly Storage _storage;
        private readonly Subject<object> _disposeSubject = new Subject<object>();

        protected AbstractReducer(IModuleToken token, Storage storage) {
            _id = token.Id;
            _storage = storage;
            _storage.RegisterReducer(_id, this);
            InitializeState();
        }


        protected ImmutableDictionary<string, object> Set<TMember>(Expression<Func<TState, TMember>> expression,
            ImmutableDictionary<string, object> state,
            TMember value) {
            return state.SetItem(GetKey(expression), value);
        }

        protected ImmutableDictionary<string, object> Set<TMember>(Expression<Func<TState, string>> expression,
            ImmutableDictionary<string, object> state,
            TMember value) {
            return state.SetItem(GetKey(expression), value);
        }

        protected string GetKey<TMember>(Expression<Func<TState, TMember>> expression) {
            return _id + (expression.Body is MemberExpression member ? member.Member.Name : "");
        }

        public IObservable<TMember> Select<TMember>(Expression<Func<TState, TMember>> expression) {
            return _storage.CreateSelector<TMember>(GetKey(expression))().TakeUntil(_disposeSubject);
        }


        public void Dispatch(IAction action) {
            if (action is ModuleScopeAction moduleScopeAction) {
                moduleScopeAction.Id = _id;
            }

            _storage.Store.Dispatch(action);
        }

        public void DispatchSetValueAction<TMember>(Expression<Func<TState, TMember>> expression, TMember value) {
            Dispatch(new Storage.SetValueAction(GetKey(expression), value));
        }

        private void InitializeState() {
            var localState = new TState();
            var state = _storage.Store.GetState();
            state = localState.GetType().GetProperties().Aggregate(state,
                (current, propertyInfo) => current.SetItem(propertyInfo.Name, propertyInfo.GetValue(localState)));
            Dispatch(new Storage.SetupStateAction(state));
        }

        public void Dispose() {
            _storage.UnregisterReducer(_id);
            _disposeSubject.OnNext(1);
            _disposeSubject.OnCompleted();
        }

        public abstract ImmutableDictionary<string, object> Reduce(ImmutableDictionary<string, object> state,
            IAction action);
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Redux;
using TeacherAssistant.Core.State;

namespace TeacherAssistant.Core.Effects
{
    public abstract class SimpleEffects<T> : IDisposable
    {
        private readonly SimpleEffectsMiddleware<T> _effects;
        private readonly Storage _storage;
        private readonly Subject<object> _destroySubject = new Subject<object>();

        public SimpleEffects(SimpleEffectsMiddleware<T> effects, Storage storage)
        {
            _effects = effects;
            _storage = storage;
        }


        public IDisposable CreateEffect(Func<IObservable<IAction>, IObservable<IAction>> runEffect)
        {
            return runEffect(this._effects.ActionStream).Subscribe(action => { _storage.Store.Dispatch(action); });
        }

        public IDisposable CreateEffect(Func<IObservable<IAction>, IObservable<IEnumerable<IAction>>> runEffect)
        {
            return runEffect(this._effects.ActionStream)
                .Subscribe(actions =>
                {
                    foreach (var action in actions)
                    {
                        _storage.Store.Dispatch(action);
                    }
                });
        }

        public void Dispose()
        {
            _destroySubject.OnNext(false);
            _destroySubject.OnCompleted();
        }
    }
}

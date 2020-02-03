using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Redux;

namespace TeacherAssistant.Core.Effects {
    public abstract class SimpleEffects<T> : IDisposable {
        private readonly SimpleEffectsMiddleware<T> _effects;
        private readonly Subject<object> _destroySubject = new Subject<object>();

        public SimpleEffects(SimpleEffectsMiddleware<T> effects) {
            _effects = effects;
        }


        public IDisposable CreateEffect(Func<IObservable<IAction>, IObservable<IAction>> runEffect) {
            return _effects.ActionStream
                .Select(tuple => {
                    var (dispatch, action) = tuple;
                    return runEffect(Observable.Return(action)).Select(effectAction => (dispatch, effectAction));
                })
                .Switch()
                .Subscribe(tuple => {
                    var (dispatch, effectAction) = tuple;
                    dispatch(effectAction);
                });
        }

        public IDisposable CreateEffect(Func<IObservable<IAction>, IObservable<IEnumerable<IAction>>> runEffect) {
            return _effects.ActionStream.TakeUntil(_destroySubject).Select(tuple => {
                    var (dispatch, action) = tuple;
                    return runEffect(Observable.Return(action))
                        .Select(effectActions => (dispatch, effectActions));
                })
                .Switch()
                .Subscribe(tuple => {
                    var (dispatch, effectActions) = tuple;
                    foreach (var effectAction in effectActions) {
                        dispatch(effectAction);
                    }
                });
        }

        public void Dispose() {
            _destroySubject.OnNext(false);
            _destroySubject.OnCompleted();
        }
    }
}
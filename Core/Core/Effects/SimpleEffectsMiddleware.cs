using System;
using System.Reactive.Subjects;
using Redux;

namespace TeacherAssistant.Core.Effects {
    public class SimpleEffectsMiddleware<T> {
        public Subject<(Func<IAction, IAction>, IAction)> ActionStream { get; } =
            new Subject<(Func<IAction, IAction>, IAction)>();

        public Middleware<T> RegisterMiddleware() => store => {
            return next => action => {
                this.ActionStream.OnNext((store.Dispatch, action));
                return next(action);
            };
        };
    }
}
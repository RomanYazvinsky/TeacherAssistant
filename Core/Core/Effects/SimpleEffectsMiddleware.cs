using System;
using System.Reactive.Subjects;
using Redux;

namespace TeacherAssistant.Core.Effects
{
    public class SimpleEffectsMiddleware<T>: IDisposable
    {
        public Subject<IAction> ActionStream { get; } =
            new Subject<IAction>();

        public Middleware<T> RegisterMiddleware() => store =>
        {
            return next => action =>
            {
                this.ActionStream.OnNext(action);
                return next(action);
            };
        };

        public void Dispose()
        {
            ActionStream?.Dispose();
        }
    }
}

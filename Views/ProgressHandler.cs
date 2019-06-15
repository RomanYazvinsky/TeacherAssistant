using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace TeacherAssistant.ComponentsImpl
{
    public class ProgressHandler
    {
        /*private Func<BehaviorSubject<int>, CancellationToken, T> _func;

        public CancellationTokenSource Aborter { get; }
        public BehaviorSubject<int> Step { get; }
        public Subject<bool> Disposer { get; }
        public int Maximum { get; }

        public ProgressHandler(Func<BehaviorSubject<int>, CancellationToken, T> func, int maximum)
        {
            _func = func;
            Maximum = maximum;
            Aborter = new CancellationTokenSource();
            Step = new BehaviorSubject<int>(0);
            Disposer = new Subject<bool>();
        }

        public async Task<T> Start()
        {
            Step.TakeUntil(Disposer).TakeWhile(i => i == Maximum).Do(i =>
            {

            })
            return await Task.Run(() => _func(Step, Aborter.Token));
        }*/
    }
}
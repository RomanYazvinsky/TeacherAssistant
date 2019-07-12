using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace TeacherAssistant.Footer.TaskExpandList
{
    public class TaskActions
    {
        public BehaviorSubject<int> CurrentValue { get; }

        public CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();

        public TaskActions(BehaviorSubject<int> currentValue, int maximum)
        {
            CurrentValue = currentValue;
            Maximum = maximum;
        }

        public int Maximum { get; }

        public void Next()
        {
            if (this.CurrentValue.Value == this.Maximum)
            {
                Complete();
                return;
            }
            this.CurrentValue.OnNext(this.CurrentValue.Value + 1);
        }

        public void Complete()
        {
            this.CurrentValue.OnCompleted();
        }
        public void ConfirmCancel()
        {
            Complete();
        }


        public bool IsCancelled()
        {
            return CancellationToken.IsCancellationRequested;
        }
    }

    public class TaskHandler
    {
        public int Maximum { get; }
        public BehaviorSubject<int> CurrentValue { get; } = new BehaviorSubject<int>(0);
        public IObservable<int> Value => CurrentValue.AsObservable();
        public bool IsIndeterminate { get; }= false;
        public bool IsCancelable { get; } = true;
        public string Name { get; }
        private Action<TaskActions> _task;
        private TaskActions _taskActions;

        public TaskHandler(string name, int maximum, bool isCancelable, Action<TaskActions> task)
        {
            this.Name = name;
            this.Maximum = maximum;
            _task = task;
            _taskActions = new TaskActions(CurrentValue, maximum);
        }

        public TaskHandler(string name, bool isCancelable, Action<TaskActions> task)
        {
            this.IsIndeterminate = true;
            this.Maximum = 100;
            this.Name = name;
            this.IsCancelable = isCancelable;
            _task = task;
            _taskActions = new TaskActions(this.CurrentValue, this.Maximum);
        }

        public void Start()
        {
            _task(_taskActions);
        }

        public void Cancel()
        {
            _taskActions.CancellationToken.Cancel();
        }

        public void Abort()
        {
            _taskActions.CancellationToken.Cancel(true);
        }

        public void Pause()
        {

        }
        
    }
}
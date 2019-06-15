using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace TeacherAssistant.Footer.TaskExpandList
{
    public class TaskActions
    {
        public BehaviorSubject<int> CurrentValue { get; }

        public CancellationTokenSource CancellationToken { get; }

        public int Maximum { get; }

        public void Next()
        {
            if (CurrentValue.Value == Maximum) return;
            CurrentValue.OnNext(CurrentValue.Value + 1);
        }

        public void Complete()
        {
            CurrentValue.OnCompleted();
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
        public BehaviorSubject<int> CurrentValue { get; }
        public IObservable<int> Value => CurrentValue.AsObservable();
        public string Name { get; }
        private Action<TaskActions> _task;
        private TaskActions _taskActions;

        public TaskHandler(string name, int maximum, Action<TaskActions> task)
        {
            Name = name;
            Maximum = maximum;
            _task = task;
            _taskActions = new TaskActions
            {

            };
        }

        public void Start()
        {
            _task(_taskActions);
        }

        public void Cancel()
        {
        }

        public void Abort()
        {
        }

        public void Pause()
        {

        }
        
    }
}
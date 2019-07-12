using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TeacherAssistant.ComponentsImpl {
    public class CommandHandler : ICommand {
        private readonly Func<Task> _asyncAction;

        public CommandHandler(Action action) {
            _asyncAction = () => {
                action();
                return Task.CompletedTask;
            };
        }

        public CommandHandler(Func<Task> action) {
            this._asyncAction = action;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) {
            _asyncAction();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class CommandHandler<T> : ICommand where T : class {
        private readonly Func<T, Task> _asyncAction;

        public CommandHandler(Action<T> action) {
            _asyncAction = (t) => {
                action(t);
                return Task.CompletedTask;
            };
        }

        public CommandHandler(Func<T, Task> action) {
            _asyncAction = action;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) {
            _asyncAction(parameter as T);
        }

        public event EventHandler CanExecuteChanged;
    }
}
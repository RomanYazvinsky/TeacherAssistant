using System;
using System.Windows.Input;

namespace TeacherAssistant.CommonButton
{
    public class CommandHandler : ICommand
    {
        private Action _action;
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public CommandHandler(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged;
    }
}
using System;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.CommonButton
{
    public class CommonButtonModel : AbstractModel
    {
        private string _text;
        private CommandHandler _action;

        public CommonButtonModel(string id) : base(id)
        {
            S<string>(id + ".Text", text => Text = text);
            S<Action>(id + ".Action", action =>
            {
                if (action == null)
                {
                    return;
                }
                Action = new CommandHandler(action);
            });
        }

        public CommandHandler Action
        {
            get => _action;
            set
            {
                _action = value;
                OnPropertyChanged(nameof(Action));
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
    }
}
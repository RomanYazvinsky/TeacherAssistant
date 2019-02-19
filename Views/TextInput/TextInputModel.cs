using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.TextInput
{
    public class TextInputModel: AbstractModel
    {
        private string _text;

        public TextInputModel(string id): base(id)
        {
            S<string>(id + ".SetText", s => Text = s);
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                Publisher.Publish(_id + ".Text", _text);
                OnPropertyChanged(nameof(Text));
            }
        }
    }
}
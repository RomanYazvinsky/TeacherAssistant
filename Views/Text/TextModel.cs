using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Text
{
    public class TextModel : AbstractModel
    {
        private string _text;

        public TextModel(string id) : base(id)
        {
            S<string>(id + ".Text", newText => Text = newText);
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
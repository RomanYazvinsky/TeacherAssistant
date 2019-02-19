using System.Windows.Controls;
using TeacherAssistant.Components;

namespace TeacherAssistant.TextInput
{
    public class TextInputComponentFactory : AbstractViewComponentFactory
    {
        public TextInputComponentFactory()
        {
            ComponentType = "TextInput";
        }

        public override UserControl GetLayout(string id)
        {
            return new TextInput(id);
        }
    }
}
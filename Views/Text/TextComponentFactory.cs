using System.Windows.Controls;
using TeacherAssistant.Components;

namespace TeacherAssistant.Text
{
    public class TextComponentFactory : AbstractViewComponentFactory
    {
        public TextComponentFactory()
        {
            ComponentType = "Text";
        }

        public override UserControl GetLayout(string id)
        {
            return new Text(id);
        }
    }
}
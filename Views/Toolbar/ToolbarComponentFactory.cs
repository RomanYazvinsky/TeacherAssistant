using System.Windows.Controls;
using TeacherAssistant.Components;

namespace TeacherAssistant.Toolbar
{
    public class ToolbarComponentFactory : AbstractViewComponentFactory
    {
        public ToolbarComponentFactory()
        {
            ComponentType = "Toolbar";
        }

        public override UserControl GetLayout(string id)
        {
            return new Toolbar();
        }
    }
}
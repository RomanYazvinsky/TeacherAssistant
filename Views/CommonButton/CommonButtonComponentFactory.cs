using System.Windows.Controls;
using TeacherAssistant.Components;

namespace TeacherAssistant.CommonButton
{
    public class CommonButtonComponentFactory : AbstractViewComponentFactory
    {
        public CommonButtonComponentFactory()
        {
            ComponentType = "Button";
        }

        public override UserControl GetLayout(string id)
        {
            return new CommonButton(id);
        }
    }
}
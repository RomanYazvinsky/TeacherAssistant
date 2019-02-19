using System.Windows.Controls;

namespace TeacherAssistant.Components.Combobox
{
    public class FilterComponentFactory : GenericViewComponentFactory
    {

        public FilterComponentFactory()
        {
            ComponentType = "Combobox";
        }

        public override UserControl GetLayout<T>(string id)
        {
            return GenericComboboxControl.Build<T>(id, true);
        }
    }
}

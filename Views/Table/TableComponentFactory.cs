using System.Windows.Controls;

namespace TeacherAssistant.Components.Table
{
    public class StudentListViewComponentFactory : GenericViewComponentFactory
    {
        public StudentListViewComponentFactory()
        {
            ComponentType = "Table";
        }

        public override UserControl GetLayout<T>(string id)
        {
            return GenericTableControl.Build<T>(id);
        }
    }
}
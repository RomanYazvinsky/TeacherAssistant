using System.Windows.Controls;

namespace TeacherAssistant.Components.Photo
{
    public class PhotoComponentFactory :AbstractViewComponentFactory
    {
        public PhotoComponentFactory()
        {
            ComponentType = "Photo";
        }
        
        public override UserControl GetLayout(string id)
        {
            return new Photo(id);
        }
    }
}
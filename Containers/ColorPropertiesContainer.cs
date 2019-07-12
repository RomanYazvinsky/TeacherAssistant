using System.Windows.Media;
using TeacherAssistant.ComponentsImpl;

namespace Containers
{
    public class ColorPropertiesContainer : SafeDictionary<Brush>
    {
        public override Brush GetDefault(string key)
        {
            return new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
    }
}
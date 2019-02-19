using System.Windows.Controls;

namespace TeacherAssistant.Components.Logger
{
    public class LoggerComponentFactory : AbstractViewComponentFactory
    {
        public override UserControl GetLayout(string id)
        {
            return new LoggerLayout(id);
        }

        public LoggerComponentFactory()
        {
            ComponentType = "Logger";
        }

    }
}
using System.Windows.Controls;

namespace TeacherAssistant
{
    public class Tab
    {
        public string Id { get; set; }
        public string Header { get; set; } = "";
        public UserControl Component { get; set; } = new UserControl();
    }
}
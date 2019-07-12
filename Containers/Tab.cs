using System.Collections.Generic;
using System.Windows.Controls;

namespace TeacherAssistant
{
    public enum ContentHolder
    {
        Modal, Tab
    }

    public class Tab
    {
        public string Id { get; set; }
        public string Header { get; set; } = "";
        public int MinHeight { get; set; } = 600;
        public UserControl Component { get; set; } = new UserControl();
        public bool HasState { get; set; } = false;
        public ContentHolder ContentHolder { get; set; } = ContentHolder.Tab;
        public Tab Previous { get; set; }
        public Tab Next { get; set; }
    }
}
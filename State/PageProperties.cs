using System;

namespace TeacherAssistant.State
{
    public class PageProperties
    {
        public Type PageType { get; set; }
        public string Header { get; set; } = "";
        public int? MinHeight { get; set; } = 720;
        public int? MaxHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MinWidth { get; set; } = 1280;
        public int? DefaultWidth { get; set; } 
        public int? DefaultHeight { get; set; }
    }
}
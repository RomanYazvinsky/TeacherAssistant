using System;
using System.ComponentModel;

namespace TeacherAssistant.State
{
    public interface IPageProperties {
        string Header { get; set; }
        int? MinHeight { get; set; }
        int? MaxHeight { get; set; }
        int? MaxWidth { get; set; }
        int? MinWidth { get; set; }
        int? DefaultWidth { get; set; }
        int? DefaultHeight { get; set; }
        
        Type Type { get; }
    }

    public class PageProperties<T> : IPageProperties
    {
    public string Header { get; set; } = "";
    public int? MinHeight { get; set; } = 720;
    public int? MaxHeight { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinWidth { get; set; } = 1280;
    public int? DefaultWidth { get; set; }
    public int? DefaultHeight { get; set; }

    public Type Type => typeof(T);
    }
}
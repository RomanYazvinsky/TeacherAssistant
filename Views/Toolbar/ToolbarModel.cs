using System;
using System.ComponentModel;

namespace TeacherAssistant.Toolbar
{
    public class ToolbarModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Dispose()
        {
        }
    }
}
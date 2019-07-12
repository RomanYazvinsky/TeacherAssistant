using System;
using System.Windows.Data;

namespace TeacherAssistant.ComponentsImpl
{
    public interface TypedValueConverter : IValueConverter
    {
        Type type1 { get; }
        Type type2 { get; }
    }
}
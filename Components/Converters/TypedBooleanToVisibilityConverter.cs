using System;
using System.Globalization;
using System.Windows;

namespace TeacherAssistant.ComponentsImpl
{
    public class TypedBooleanToVisibilityConverter: TypedValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }
            throw new Exception("Expected value to be boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is Visibility booleanValue)
            {
                return booleanValue == Visibility.Visible;
            }
            throw new Exception("Expected value to be Visibility");
        }

        public Type type1 { get; } = typeof(Visibility);
        public Type type2 { get; } = typeof(bool);
    }
}
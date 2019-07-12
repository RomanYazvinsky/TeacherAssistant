using System;
using System.Windows.Data;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.StudentForm
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : TypedValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion

        public Type type1 { get; } = typeof(bool);
        public Type type2 { get; } = typeof(bool);
    }
}
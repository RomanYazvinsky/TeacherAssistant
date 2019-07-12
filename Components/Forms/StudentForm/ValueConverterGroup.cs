using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.StudentForm
{
    public class ValueConverterGroup : List<TypedValueConverter>, IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return this.Aggregate(value, (current, converter) =>
            {
                return converter.Convert(current, converter.type1, parameter, culture);
            });
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var list = new List<TypedValueConverter>(this);
            list.Reverse();
            return list.Aggregate(value, (current, converter) => converter.Convert(current, converter.type2, parameter, culture));
        }

        #endregion
    }
}
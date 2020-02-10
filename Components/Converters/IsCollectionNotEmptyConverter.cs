using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TeacherAssistant {
    public class IsCollectionNotEmptyConverter : IValueConverter {
        public object TrueValue { get; }
        public object FalseValue { get; }

        public IsCollectionNotEmptyConverter() {
            TrueValue = true;
            FalseValue = false;
        }

        public IsCollectionNotEmptyConverter(object trueValue, object falseValue) {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            switch (value) {
                case null: {
                    return FalseValue;
                }
                case ICollection e:
                    return e.Count > 0 ? TrueValue : FalseValue;
                case int i when i > 0:
                case long l when l > 0:
                    return TrueValue;
                default:
                    return FalseValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException("Conversion is not supported");
        }
    }

    public class IsCollectionVisible : IsCollectionNotEmptyConverter {
        public IsCollectionVisible() : base(Visibility.Visible, Visibility.Collapsed) {
        }
    }
}

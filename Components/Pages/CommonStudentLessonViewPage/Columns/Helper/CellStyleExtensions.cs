using System.Windows;
using System.Windows.Media;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Columns.Helper {
    public static class CellStyleExtensions {
        public static readonly DependencyProperty IsRegisteredProperty = DependencyProperty.RegisterAttached(
            "IsRegistered",
            typeof(bool?),
            typeof(CellStyleExtensions),
            new FrameworkPropertyMetadata(defaultValue: true)
        );

        public static readonly DependencyProperty ContentBackgroundProperty = DependencyProperty.RegisterAttached(
            "ContentBackground",
            typeof(Brush),
            typeof(CellStyleExtensions),
            new FrameworkPropertyMetadata(defaultValue: Brushes.Transparent)
        );

        public static bool GetIsRegistered(DependencyObject obj) {
            return (bool) obj.GetValue(IsRegisteredProperty);
        }

        public static void SetIsRegistered(DependencyObject obj, bool value) {
            obj.SetValue(IsRegisteredProperty, value);
        }

        public static void SetContentBackground(DependencyObject obj, Brush value) {
            obj.SetValue(ContentBackgroundProperty, value);
        }

        public static Brush GetContentBackground(DependencyObject obj) {
            return (Brush) obj.GetValue(ContentBackgroundProperty);
        }
    }
}
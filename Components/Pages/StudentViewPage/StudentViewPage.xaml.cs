using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.StudentViewPage
{
    /// <summary>
    /// Interaction logic for StudentViewPage.xaml
    /// </summary>
    public partial class StudentViewPage : View<StudentViewPageModel> {
        public StudentViewPage(string id)
        {
            InitializeComponent();
            InitializeViewModel(id);
        }

        private void LessonMarkMouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is TextBox box)) return;
            box.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            box.BorderThickness = new Thickness(2);
        }

        private void LessonMarkMouseLeave(object sender, object e)
        {
            if (!(sender is TextBox box) || box.IsFocused) return;
            box.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            box.BorderThickness = new Thickness(0);
        }

        private void LessonMarkLostFocus(object sender, object e)
        {
            if (!(sender is TextBox box)) return;
            box.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            box.BorderThickness = new Thickness(0);
        }

        private void ValidateExamMark(object sender, TextCompositionEventArgs e)
        {
            /*var text = ((TextBox) sender).Text;
            if (text.Equals("") || text.Equals("+") || text.Equals("-")) return;
            if (int.TryParse(text, out var i) && i <= 10 && i >= 0)
            {
                return;
            }

            e.Handled = true;*/
        }
    }
}
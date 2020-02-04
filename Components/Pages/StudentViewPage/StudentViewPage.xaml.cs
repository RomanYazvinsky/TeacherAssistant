using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.StudentViewPage {
    public class StudentViewPageToken : PageModuleToken<StudentViewPageModule> {
        public StudentViewPageToken(string title, StudentEntity student) : base(title) {
            this.Student = student;
        }

        public StudentEntity Student { get; }
    }

    public class StudentViewPageModule : SimpleModule {
        public StudentViewPageModule() : base(typeof(StudentViewPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportModuleScope<StudentViewPageModel>();
            block.ExportModuleScope<StudentViewPage>()
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class StudentViewPageBase : View<StudentViewPageToken, StudentViewPageModel> {
    }

    /// <summary>
    /// Interaction logic for StudentViewPage.xaml
    /// </summary>
    public partial class StudentViewPage : StudentViewPageBase {
        public StudentViewPage() {
            InitializeComponent();
        }

        private void LessonMarkMouseEnter(object sender, MouseEventArgs e) {
            if (!(sender is TextBox box)) return;
            box.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            box.BorderThickness = new Thickness(2);
        }

        private void LessonMarkMouseLeave(object sender, object e) {
            if (!(sender is TextBox box) || box.IsFocused) return;
            box.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            box.BorderThickness = new Thickness(0);
        }

        private void LessonMarkLostFocus(object sender, object e) {
            if (!(sender is TextBox box)) return;
            box.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            box.BorderThickness = new Thickness(0);
        }

        private void ValidateExamMark(object sender, TextCompositionEventArgs e) {
            /*var text = ((TextBox) sender).Text;
            if (text.Equals("") || text.Equals("+") || text.Equals("-")) return;
            if (int.TryParse(text, out var i) && i <= 10 && i >= 0)
            {
                return;
            }

            e.Handled = true;*/
        }

        private void ExternalLessonClick(object sender, MouseButtonEventArgs e) {
            this.ViewModel.OpenExternalLesson?.Command?.Execute(sender);
        }
    }
}

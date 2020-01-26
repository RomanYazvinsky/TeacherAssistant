using System.Windows;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.StudentForm {
    /// <summary>
    /// Interaction logic for StudentForm.xaml
    /// </summary>
    public partial class StudentForm : View<StudentFormModel> {
        public StudentForm(string id) {
            InitializeComponent();
        }

        private void OnExit(object sender, RoutedEventArgs e) {
            this.ViewModel.Dispose();
        }
    }
}
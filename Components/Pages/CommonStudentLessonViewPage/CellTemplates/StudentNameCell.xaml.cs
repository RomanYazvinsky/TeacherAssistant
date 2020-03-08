using System.Linq;
using System.Windows;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Services;
using TeacherAssistant.StudentViewPage;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.CellTemplates {
    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class StudentNameCell {
        public StudentNameCell() {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            if (Popup.IsOpen) {
                Popup.IsOpen = false;
                return;
            }

            if (!(this.DataContext is StudentRowViewModel view)) {
                return;
            }

            var service = view.ServiceLocator.Locate<PhotoService>();
            var path = await service.DownloadPhoto(StudentEntity.CardUidToId(view.Student.CardUid));
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            Image.Source = service.GetImage(path);
            Popup.IsOpen = true;
        }

        private void OpenStudent(object sender, RoutedEventArgs e) {
            if (!(this.DataContext is StudentRowViewModel view)) {
                return;
            }

            var tabPageHost = view.ServiceLocator.Locate<TabPageHost>();
            tabPageHost.AddPageAsync(new StudentViewPageToken("Студент", view.Student));
        }

        private void OpenNotes(object sender, RoutedEventArgs e) {
            if (!(this.DataContext is StudentRowViewModel view)) {
                return;
            }

            var windowPageHost = view.ServiceLocator.Locate<WindowPageHost>();
            windowPageHost.AddPageAsync(new NoteListFormToken(
                "Заметки",
                () => new StudentNote {
                    EntityId = view.Student.Id,
                    Student = view.Student
                }, view.Student.Notes));
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!(e.NewValue is StudentRowViewModel context)) {
                return;
            }

            NoteInfoIcon.Visibility = context.Student.Notes?.Any() == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
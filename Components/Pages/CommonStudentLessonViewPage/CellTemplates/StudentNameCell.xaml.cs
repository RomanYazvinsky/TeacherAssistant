using System.Windows;
using System.Windows.Controls;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.Services;
using TeacherAssistant.StudentViewPage;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.CellTemplates
{
    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class StudentNameCell : UserControl
    {
        public StudentNameCell()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Popup.IsOpen)
            {
                Popup.IsOpen = false;
                return;
            }

            if (!(this.DataContext is StudentLessonViewModel view))
            {
                return;
            }

            var service = view.ServiceLocator.Locate<PhotoService>();
            var path = await service.DownloadPhoto(StudentEntity.CardUidToId(view.Model.CardUid));
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Image.Source = service.GetImage(path);
            Popup.IsOpen = true;
        }

        private void OpenStudent(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is StudentLessonViewModel view))
            {
                return;
            }

            var tabPageHost = view.ServiceLocator.Locate<TabPageHost>();
            tabPageHost.AddPageAsync(new StudentViewPageToken("Студент", view.Model));
        }
    }
}

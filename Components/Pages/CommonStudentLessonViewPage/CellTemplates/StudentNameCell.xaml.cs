using System.Windows;
using System.Windows.Controls;
using Model.Models;
using TeacherAssistant.Components;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.CellTemplates {
    /// <summary>
    /// Interaction logic for CommonStudentLessonViewPage.xaml
    /// </summary>
    public partial class StudentNameCell : UserControl {
        public StudentNameCell() {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            if (Popup.IsOpen) {
                Popup.IsOpen = false;
                return;
            }

            if (!(this.DataContext is StudentLessonView view)) {
                return;
            }

            // var service = Injector.Instance.Kernel.Get<PhotoService>();
            // var path = await service.DownloadPhoto(StudentEntity.CardUidToId(view.Model.CardUid));
            // if (string.IsNullOrWhiteSpace(path)) {
                // return;
            // }

            // Image.Source = service.GetImage(path);
            // Popup.IsOpen = true;
        }

        private void OpenStudent(object sender, RoutedEventArgs e) {
            if (!(this.DataContext is StudentLessonView view)) {
                return;
            }

            // var activation = Injector.Instance.Kernel.Get<TableLessonViewToken>();
            // var pageHost = Injector.Instance.Kernel.Get<IPageHost>();
            // var pageId = pageService.OpenPage(new PageProperties<StudentViewPage.StudentViewPage> {
            //     Header = view.FullName
            // }, activation.Id);
            // StoreManager.Publish(view.Model, pageId, "Student");
        }
    }
}
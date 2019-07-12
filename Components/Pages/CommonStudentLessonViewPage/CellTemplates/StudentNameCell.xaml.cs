using System.Windows;
using System.Windows.Controls;
using Model.Models;
using Ninject;
using TeacherAssistant.Components;
using TeacherAssistant.State;

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

            var service = (IPhotoService) Injector.Instance.Kernel.Get(typeof(IPhotoService));
            var path = await service.DownloadPhoto(StudentModel.CardUidToId(view.Model.CardUid));
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            Image.Source = service.GetImage(path);
            Popup.IsOpen = true;
        }

        private void OpenStudent(object sender, RoutedEventArgs e) {
            if (!(this.DataContext is StudentLessonView view)) {
                return;
            }

            var pageService = Injector.Instance.Kernel.Get<PageService>();
            var pageId = pageService.OpenPage(new PageProperties {
                PageType = typeof(StudentViewPage.StudentViewPage),
                Header = view.FullName
            }, view.TableId);
            StoreManager.Publish(view.Model, pageId, "Student");
        }
    }
}
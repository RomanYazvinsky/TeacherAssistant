using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.RegistrationPage {
    public class RegistrationPageBase : View<RegistrationPageModel> {
    }

    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : RegistrationPageBase {
        public RegistrationPage(string id) {
            InitializeComponent();
            InitializeViewModel(id);
            this.WhenActivated(action => {
                this.OneWayBind(this.ViewModel, model => model.TimerString, page => page.TimeBox.Text)
                    .DisposeWith(action);
                this.Bind(this.ViewModel, model => model.IsAutoRegistrationEnabled,
                        page => page.AutoRegBox.IsChecked)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentPhoto,
                        page => page.StudentPhoto.Source)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ActiveStudentInfoVisibility,
                        page => page.StudentPhoto.Visibility)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription,
                        page => page.StudentDescription.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ActiveStudentInfoVisibility,
                        page => page.StudentDescription.Visibility)
                    .DisposeWith(action);
                this.Bind(this.ViewModel, model => model.SelectedStudent,
                        page => page.LessonStudents.SelectedItem)
                    .DisposeWith(action);
                this.Bind(this.ViewModel, model => model.SelectedStudent,
                        page => page.RegisteredStudents.SelectedItem)
                    .DisposeWith(action);
                this.BindCommand(this.ViewModel, model => model.DoRegister, page => page.RegisterButton)
                    .DisposeWith(action);
                this.BindCommand(this.ViewModel, model => model.DoUnRegister, page => page.UnregisterButton)
                    .DisposeWith(action);
            });
            LessonStudents.FilterFunction = this.ViewModel.Filter;
            LessonStudents.Sorts = this.ViewModel.Sorts;
            LessonStudents.SelectedItems = this.ViewModel.SelectedLessonStudents;
            LessonStudents.TableItems = this.ViewModel.LessonStudents;
            RegisteredStudents.FilterFunction = this.ViewModel.Filter;
            RegisteredStudents.Sorts = this.ViewModel.RegisteredStudentsSorts;
            RegisteredStudents.SelectedItems = this.ViewModel.SelectedRegisteredStudents;
            RegisteredStudents.TableItems = this.ViewModel.RegisteredStudents;
            RegisteredStudents.DropAvailability = this.ViewModel.RegisteredDropAvailability;
            LessonStudents.DropAvailability = this.ViewModel.LessonStudentsTableDropAvailability;
            RegisteredStudents.Drop = this.ViewModel.DropOnRegisteredStudents;
            LessonStudents.Drop = this.ViewModel.DropOnLessonStudents;
            RegisteredStudents.DragStart = this.ViewModel.DragStartRegisteredStudents;
            LessonStudents.DragStart = this.ViewModel.DragStartLessonStudents;
        }

        private void List_OnMouseEnter(object sender, MouseEventArgs e) {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject) e.Source, ScrollBarVisibility.Auto);
        }

        private void List_OnMouseLeave(object sender, MouseEventArgs e) {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject) e.Source, ScrollBarVisibility.Hidden);
        }
    }
}
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Model.Models;
using Ninject;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;
using TeacherAssistant.State;

namespace TeacherAssistant.RegistrationPage {
    public class RegistrationPageModuleToken : PageModuleToken<RegistrationPageModule> {
        public RegistrationPageModuleToken(string title, LessonEntity lesson) :
            base(IdGenerator.GenerateId(), title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class RegistrationPageModule : Module {
        public RegistrationPageModule()
            : base(new[] {
                typeof(RegistrationPage),
                typeof(RegistrationPageModel),
            }) {
        }

        public override Control GetEntryComponent() {
            return this.Kernel?.Get<RegistrationPage>();
        }
    }

    public class RegistrationPageBase : View<RegistrationPageModel> {
    }

    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : RegistrationPageBase {
        public RegistrationPage() {
            InitializeComponent();
        }

        public override void Initialize() {
            this.WhenActivated(action => {
                this.OneWayBind(this.ViewModel, model => model.TimerString, page => page.TimeBox.Text)
                    .DisposeWith(action);
                this.Bind(this.ViewModel, model => model.IsAutoRegistrationEnabled,
                        page => page.AutoRegBox.IsChecked)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.Photo,
                        page => page.StudentPhoto.Source)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ActiveStudentInfoVisibility,
                        page => page.StudentPhoto.Visibility)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.LastName,
                        page => page.LastNameText.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.FirstName,
                        page => page.FirstNameText.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.SecondName,
                        page => page.SecondNameText.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.LessonStat,
                        page => page.LessonStatText.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.StudentDescription.GroupName,
                        page => page.GroupNamesText.Text)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ActiveStudentInfoVisibility,
                        page => page.StudentDescription.Visibility)
                    .DisposeWith(action);
                this.BindCommand(this.ViewModel, model => model.DoRegister, page => page.RegisterButton)
                    .DisposeWith(action);
                this.BindCommand(this.ViewModel, model => model.DoUnRegister, page => page.UnregisterButton)
                    .DisposeWith(action);
                this.Bind(this.ViewModel, model => model.AllStudentsTableConfig, page => page.AllStudents.TableConfig)
                    .DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ToggleAllStudentTable.Text,
                    page => page.ToggleAllStudentsMode.Content).DisposeWith(action);
                this.OneWayBind(this.ViewModel, model => model.ToggleAllStudentTable.Command,
                    page => page.ToggleAllStudentsMode.Command).DisposeWith(action);
            });
            LessonStudents.TableConfig = this.ViewModel.LessonStudentsTableConfig;
            RegisteredStudents.TableConfig = this.ViewModel.RegisteredStudentsTableConfig;
        }

        private void List_OnMouseEnter(object sender, MouseEventArgs e) {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject) e.Source, ScrollBarVisibility.Auto);
        }

        private void List_OnMouseLeave(object sender, MouseEventArgs e) {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject) e.Source, ScrollBarVisibility.Hidden);
        }
    }
}
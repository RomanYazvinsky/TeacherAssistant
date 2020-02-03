﻿using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Grace.DependencyInjection;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Core.Module;

namespace TeacherAssistant.RegistrationPage {
    public class RegistrationPageToken : PageModuleToken<RegistrationPageModule> {
        public RegistrationPageToken(string title, LessonEntity lesson) :
            base(title) {
            this.Lesson = lesson;
        }

        public LessonEntity Lesson { get; }
    }

    public class RegistrationPageModule : SimpleModule {
        public RegistrationPageModule() : base(typeof(RegistrationPage)) {
        }

        public override void Configure(IExportRegistrationBlock block) {
            block.ExportInitialize<IInitializable>(initializable => initializable.Initialize());
            block.ExportModuleScope<RegistrationPageModel>(this.ModuleToken.Id);
            block.ExportModuleScope<RegistrationPage>(this.ModuleToken.Id)
                .ImportProperty(v => v.ModuleToken)
                .ImportProperty(v => v.ViewModel);
        }
    }

    public class RegistrationPageBase : View<RegistrationPageToken, RegistrationPageModel> {
    }

    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : RegistrationPageBase, IInitializable {
        public RegistrationPage() {
            InitializeComponent();
        }

        public void Initialize() {
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Model.Models;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.RegistrationPage
{
    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : UserControl
    {
        private RegistrationPageModel _model;
        public RegistrationPage(string id)
        {
            InitializeComponent();
            Injector.GetInstance().Kernel.Rebind<RegistrationPageModel>().ToSelf().InSingletonScope().WithConstructorArgument(id);
            _model = Injector.GetInstance().Kernel.Get<RegistrationPageModel>();
            DataContext = _model;

            SortAdorner.AddColumnSorting(RegisteredStudentsList);
            SortAdorner.AddColumnSorting(LessonStudentsList);
        }

        private void OnRegisteredStudentsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _model.SelectedRegisteredStudents.Clear();

            foreach (StudentLessonModel item in RegisteredStudentsList.SelectedItems)
            {
                _model.SelectedRegisteredStudents.Add(item);
            }
        }

        private void OnLessonStudentsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _model.SelectedLessonStudents.Clear();
            foreach (StudentLessonModel item in LessonStudentsList.SelectedItems)
            {
                _model.SelectedLessonStudents.Add(item);
            }
        }

        private void OnLessonStudentsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = (StudentLessonModel)LessonStudentsList.SelectedItem;
            if (selectedItem == null || (_model.LessonStudentsSelectedItem != null && selectedItem.student_id == _model.LessonStudentsSelectedItem.id))
            {
                return;
            }
            _model.LessonStudentsSelectedItem = selectedItem.Student;
        }

        private void List_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject)e.Source, ScrollBarVisibility.Auto);
        }

        private void List_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ScrollViewer.SetVerticalScrollBarVisibility((DependencyObject)e.Source, ScrollBarVisibility.Hidden);
        }

        private void Toggle_AutoRegistration(object sender, RoutedEventArgs e)
        {
            _model.IsAutoRegistrationEnabled = IsAutoRegistrationEnabled.IsChecked ?? false;
        }

        private void OnRegisteredStudentsListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = (StudentLessonModel)RegisteredStudentsList.SelectedItem;
            if (selectedItem == null || (_model.LessonStudentsSelectedItem != null && selectedItem.student_id == _model.LessonStudentsSelectedItem.id))
            {
                return;
            }
            _model.LessonStudentsSelectedItem = selectedItem.Student;
        }

        private void LessonStudentsList_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Delete))
            {
                _model.Remove((StudentLessonModel)LessonStudentsList.SelectedItem);
            }
        }
    }
}

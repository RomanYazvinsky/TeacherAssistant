using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Model.Models;

namespace TeacherAssistant.RegistrationPage
{
    /// <summary>
    /// Interaction logic for RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : UserControl
    {
        public RegistrationPage(string id)
        {
            InitializeComponent();
            DataContext = new RegistrationPageModel(id);
        }

        private void OnRegisteredStudentsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var studentModels = new ObservableCollection<StudentModel>(); 
            foreach (StudentModel selectedItem in RegisteredStudentsList.SelectedItems)
            {
                studentModels.Add(selectedItem);
            }

            ((RegistrationPageModel) DataContext).SelectedRegisteredStudents = studentModels;
        }

        private void OnLessonStudentsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var studentModels = new ObservableCollection<StudentModel>();
            foreach (StudentModel selectedItem in LessonStudentsList.SelectedItems)
            {
                studentModels.Add(selectedItem);
            }

            ((RegistrationPageModel)DataContext).SelectedLessonStudents = studentModels;
        }
    }
}

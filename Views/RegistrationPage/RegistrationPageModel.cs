using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dao;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;

namespace TeacherAssistant.RegistrationPage
{
    public class RegistrationPageModel : AbstractModel
    {
        private LessonModel _currentLesson;
        private BitmapImage _studentPhoto;
        private ObservableCollection<RegisteredStudentModel> _registeredStudents;
        private StudentModel _selectedStudent;
        private ObservableCollection<StudentModel> _selectedRegisteredStudents;
        private ObservableCollection<StudentModel> _selectedLessonStudents = new ObservableCollection<StudentModel>();
        private StudentModel _lessonStudentsSelectedItem;
        private string _activeStudentInfo;

        public RegistrationPageModel(string id) : base(id)
        {
            SimpleSubscribe<LessonModel>(id + "." + SchedulePageModel.SELECTED_LESSON, model =>
            {
                _currentLesson = model;
                _selectedLessonStudents.Clear();
                foreach (var student in from studentModel in
                        GeneralDbContext.GetInstance().StudentGroupModels.Include(sgm => sgm.Student)
                    where studentModel.group_id == _currentLesson.group_id
                    select studentModel.Student)
                {
                    _selectedLessonStudents.Add(student);
                }
            });
            SimpleSubscribe<ObservableCollection<StudentLessonModel>>(
                id + ".RegisteredStudents", data =>
                {
                    if (data == null) { return; }
                    RegisteredStudents =
                        new ObservableCollection<RegisteredStudentModel>(data.Select(model =>
                            new RegisteredStudentModel(model)));
                });
            SimpleSubscribe<StudentModel>("LastReadStudentCard", model => SelectedStudent = model);
        }

        public string ActiveStudentInfo
        {
            get => _activeStudentInfo;
            set => _activeStudentInfo = value;
        }

        public ObservableCollection<RegisteredStudentModel> RegisteredStudents
        {
            get => _registeredStudents;
            set => _registeredStudents = value;
        }

        private StudentModel SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                Task.Run(async () =>
                {
                    StudentPhoto = null;
                    var photoPath = await PhotoService.GetInstance().DownloadPhoto(value.card_id);
                    if (photoPath == null)
                    {
                        return;
                    }
                    var uriSource = new Uri(photoPath);
                    StudentPhoto = new BitmapImage(uriSource);
                });
                _selectedStudent = value;
                ActiveStudentInfo = value.last_name + " " + value.first_name + " " + (value.patronymic ?? "");
            }
        }

        public ObservableCollection<StudentModel> SelectedRegisteredStudents
        {
            get => _selectedRegisteredStudents;
            set => _selectedRegisteredStudents = value;
        }

        public StudentModel LessonStudentsSelectedItem
        {
            get => _lessonStudentsSelectedItem;
            set => _lessonStudentsSelectedItem = value;
        }

        public ObservableCollection<StudentModel> SelectedLessonStudents
        {
            get => _selectedLessonStudents;
            set => _selectedLessonStudents = value;
        }

        public BitmapImage StudentPhoto
        {
            get => _studentPhoto;
            set => _studentPhoto = value;
        }
    }
}
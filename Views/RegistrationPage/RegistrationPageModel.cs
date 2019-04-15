using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dao;
using Model.Models;
using Ninject;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.RegistrationPage
{
    public class RegistrationPageModel : AbstractModel
    {
        public ISerialUtil SerialUtil { get; set; }
        private List<StudentLessonModel> _studentLessonModels;
        private LessonModel _currentLesson;
        private StudentModel _selectedStudent;
        private StudentModel _lessonStudentsSelectedItem;
        private BitmapImage _studentPhoto;
        private string _activeStudentInfo;
        private StudentModel _accumulatedStudent;
        private bool _isUidAccepted = false;
        private ObservableCollection<StudentLessonModel> _registeredStudents =
            new ObservableCollection<StudentLessonModel>();

        private ObservableCollection<StudentLessonModel> _lessonStudents =
            new ObservableCollection<StudentLessonModel>();

        private ICommand _doRegister;

        private ICommand _doUnRegister;
        private StudentModel _registeredStudentsSelectedItem;

        [Inject]
        public RegistrationPageModel(ISerialUtil serialUtil, string id) : base(id)
        {
            SerialUtil = serialUtil;
            _doRegister = new SchedulePageModel.CommandHandler(() =>
            {
                Register(new List<StudentLessonModel>(SelectedLessonStudents));
            });
            _doUnRegister = new SchedulePageModel.CommandHandler(() =>
            {
                UnRegister(new List<StudentLessonModel>(SelectedRegisteredStudents));
            });
            SimpleSubscribe<LessonModel>(id + "." + SchedulePageModel.SELECTED_LESSON, model =>
            {
                if (model == null) return;
                _currentLesson = model;
                LessonStudents.Clear();
                RegisteredStudents.Clear();
                var studentModels = GetStudentLessonModels(model);
                AddMissingStudents(studentModels, _currentLesson);
                _studentLessonModels = studentModels;
                LessonStudents =
                    new ObservableCollection<StudentLessonModel>(
                        _studentLessonModels.Where(lessonModel =>
                            !lessonModel.registered.HasValue || lessonModel.registered.Value == 0));
                RegisteredStudents = new ObservableCollection<StudentLessonModel>(
                    _studentLessonModels.Where(lessonModel =>
                        lessonModel.registered.HasValue && lessonModel.registered.Value != 0));

            });
            SimpleSubscribe<StudentModel>("LastReadStudentCard", model => SelectedStudent = model);
            SerialUtil.DataReceivedSuccess += ReadData;
        }

        private List<StudentLessonModel> GetStudentMissedLessons(StudentModel model)
        {
            var now = DateTime.Now.Date;
            var studentLessonModels = new List<StudentLessonModel>(from x in GeneralDbContext.GetInstance().StudentLessonModels.Include(lessonModel => lessonModel.Lesson)
                                                                   where x.student_id == model.id
                                                                         && x.registered.HasValue
                                                                         && x.registered.Value == 0
                                                                         && x.Lesson.stream_id == _currentLesson.stream_id
                                                                   select x);
            if (studentLessonModels.Count == 0) return studentLessonModels;
            studentLessonModels = new List<StudentLessonModel>(studentLessonModels.Where(lessonModel => lessonModel.Lesson.Date < now));
            return studentLessonModels;
        }

        private void AccumulateUid(string data)
        {
            _accumulatedStudent = new StudentModel
            {
                card_uid = data,
                card_id = int.Parse(data, System.Globalization.NumberStyles.HexNumber).ToString()
            };
            _isUidAccepted = true;
            Task.Delay(1000).ContinueWith(task => { _isUidAccepted = false; });
        }

        private void AccumulateStudentData(string data)
        {
            if (_accumulatedStudent == null || !_isUidAccepted)
            {
                return;
            }
            int studyInfoLength = 7;
            if (!char.IsDigit(data[studyInfoLength]))
            {
                studyInfoLength--;
            }

            var studyBeginning = data.Substring(0, studyInfoLength);
            var fullName = data.Substring(studyInfoLength, data.Length - studyInfoLength).Split(' ');
            _accumulatedStudent.last_name = fullName[0];
            _accumulatedStudent.first_name = fullName[1];
            _accumulatedStudent.patronymic = fullName[2];
            UpdatePhoto(_accumulatedStudent);
            UpdateDescription(_accumulatedStudent);
            _isUidAccepted = false;
        }

        private List<StudentLessonModel> GetStudentLessonModels(LessonModel lessonModel)
        {
            return new List<StudentLessonModel>(
                from studentLessonModel in GeneralDbContext.GetInstance().StudentLessonModels
                where studentLessonModel.lesson_id == lessonModel.id
                select studentLessonModel);
        }

        private void AddMissingStudents(List<StudentLessonModel> studentLessonModels, LessonModel lessonModel)
        {
            List<StudentGroupModel> students;
            if (_currentLesson.group_id == null)
            {
                students = new List<StudentGroupModel>(
                    from studentGroupModel in GeneralDbContext.GetInstance().StudentGroupModels
                    join streamGroupModel in GeneralDbContext.GetInstance().StreamGroupModels
                        on studentGroupModel.group_id equals streamGroupModel.group_id
                    where streamGroupModel.stream_id == lessonModel.stream_id
                    select studentGroupModel);
            }
            else
            {
                students = new List<StudentGroupModel>(
                    from studentGroupModel in GeneralDbContext.GetInstance().StudentGroupModels
                    join streamGroupModel in GeneralDbContext.GetInstance().StreamGroupModels
                        on studentGroupModel.group_id equals streamGroupModel.group_id
                    where streamGroupModel.group_id == lessonModel.group_id
                    select studentGroupModel);
            }
            var newStudentLessonModels = students
                .Where(studentGroupModel =>
                    studentLessonModels.All(model => model.student_id != studentGroupModel.student_id)).Select(model =>
                    new StudentLessonModel
                    {
                        lesson_id = lessonModel.id,
                        student_id = model.student_id,
                        registered = 0
                    });
            if (students.Count <= 0) return;
            GeneralDbContext.GetInstance().StudentLessonModels.AddRange(newStudentLessonModels);
            GeneralDbContext.GetInstance().SaveChanges();
            studentLessonModels.AddRange(newStudentLessonModels);

        }

        private void ReadData(object sender, string readData)
        {
            if (readData == null) return;
            if (!readData.StartsWith("Card"))
            {
                AccumulateStudentData(readData);
                return;
            }
            var cardUid = readData.Substring(9, 8);
            var student = LessonStudents
                .FirstOrDefault(studentModel => studentModel.Student.card_uid.Equals(cardUid));
            if (student != null)
            {
                UpdatePhoto(student.Student);
                UpdateDescription(student.Student);
                if (IsAutoRegistrationEnabled)
                {
                    Register(new List<StudentLessonModel> { student });
                }
                return;
            }

            var studentFromDatabase = GeneralDbContext.GetInstance().StudentModels
                .FirstOrDefault(model => model.card_uid.Equals(cardUid));
            if (studentFromDatabase != null)
            {
                UpdatePhoto(studentFromDatabase);
                UpdateDescription(studentFromDatabase);
                if (IsAutoRegistrationEnabled)
                {
                    Application.Current.Dispatcher.Invoke(() => { RegisterExtStudent(studentFromDatabase); });
                }
                return;
            }
            AccumulateUid(cardUid);
            //var model = new StudentModel { card_uid = readData.Substring(9, 8) };
            //model.card_id = int.Parse(model.card_uid, System.Globalization.NumberStyles.HexNumber).ToString();
            //int studyInfoLength = 7;
            //var dateAndName = readData.Split('\n')[1];
            //if (!char.IsDigit(dateAndName[studyInfoLength]))
            //{
            //    studyInfoLength--;
            //}

            //var studyBeginning = dateAndName.Substring(0, studyInfoLength);
            //var fullName = dateAndName.Substring(studyInfoLength, dateAndName.Length - studyInfoLength).Split(' ');
            //model.last_name = fullName[0];
            //model.first_name = fullName[1];
            //model.patronymic = fullName[2];
        }

        private void Register(List<StudentLessonModel> models)
        {
            foreach (var studentLessonModel in models)
            {
                studentLessonModel.registered = 1;
                LessonStudents.Remove(studentLessonModel);
                if (SelectedLessonStudents.Contains(studentLessonModel))
                {
                    SelectedLessonStudents.Remove(studentLessonModel);
                }
                RegisteredStudents.Add(studentLessonModel);
            }

            GeneralDbContext.GetInstance().SaveChanges();
        }

        private void RegisterExtStudent(StudentModel studentModel)
        {
            StudentLessonModel studentLessonModel = new StudentLessonModel
            {
                lesson_id = _currentLesson.id,
                student_id = studentModel.id,
                registered = 1
            };
            RegisteredStudents.Add(studentLessonModel);
            GeneralDbContext.GetInstance().StudentLessonModels.Add(studentLessonModel);
            GeneralDbContext.GetInstance().SaveChanges();
        }

        private void UnRegister(List<StudentLessonModel> models)
        {
            foreach (var studentLessonModel in models)
            {
                studentLessonModel.registered = 0;
                RegisteredStudents.Remove(studentLessonModel);
                if (SelectedRegisteredStudents.Contains(studentLessonModel))
                {
                    SelectedRegisteredStudents.Remove(studentLessonModel);
                }
                LessonStudents.Add(studentLessonModel);
            }

            GeneralDbContext.GetInstance().SaveChanges();
        }

        private void UpdatePhoto(StudentModel model)
        {
            if (model == null) return;
            StudentPhoto = null;
            Task.Run(async () =>
            {
                var photoPath = await PhotoService.GetInstance().DownloadPhoto(model.card_id);
                if (photoPath == null)
                {
                    return;
                }

                var uriSource = new Uri(photoPath);
                Application.Current.Dispatcher.Invoke(() => { StudentPhoto = new BitmapImage(uriSource); },
                    DispatcherPriority.DataBind);
            });
        }

        private void UpdateDescription(StudentModel model)
        {
            if (model == null) return;
            ActiveStudentInfo = $"{model.last_name}  {model.first_name} {(model.patronymic ?? "")} \nПропуски: {GetStudentMissedLessons(model).Count}";
        }

        public void Remove(StudentLessonModel model)
        {
            if (model == null)
            {
                return;
            }

            LessonStudents.Remove(model);
            GeneralDbContext.GetInstance().StudentLessonModels.Remove(model);
            GeneralDbContext.GetInstance().SaveChanges();
        }

        public string ActiveStudentInfo
        {
            get => _activeStudentInfo;
            set
            {
                _activeStudentInfo = value;
                OnPropertyChanged(nameof(ActiveStudentInfo));
            }
        }

        public ObservableCollection<StudentLessonModel> RegisteredStudents
        {
            get => _registeredStudents;
            set
            {
                _registeredStudents = value;
                OnPropertyChanged(nameof(RegisteredStudents));
            }
        }

        private StudentModel SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                _selectedStudent = value;
                UpdatePhoto(value);
                UpdateDescription(value);
                OnPropertyChanged(nameof(SelectedStudent));
            }
        }

        public ObservableCollection<StudentLessonModel> SelectedRegisteredStudents { get; set; } =
            new ObservableCollection<StudentLessonModel>();
        public ObservableCollection<StudentLessonModel> SelectedLessonStudents { get; set; } =
            new ObservableCollection<StudentLessonModel>();

        public StudentModel LessonStudentsSelectedItem
        {
            get => _lessonStudentsSelectedItem;
            set
            {
                _lessonStudentsSelectedItem = value;
                UpdatePhoto(value);
                UpdateDescription(value);
            }
        }

        public StudentModel RegisteredStudentsSelectedItem
        {
            get => _registeredStudentsSelectedItem;
            set
            {
                _registeredStudentsSelectedItem = value;
                UpdatePhoto(value);
                UpdateDescription(value);
            }
        }


        public ObservableCollection<StudentLessonModel> LessonStudents
        {
            get => _lessonStudents;
            set
            {
                _lessonStudents = value;
                OnPropertyChanged(nameof(LessonStudents));
            }
        }

        public BitmapImage StudentPhoto
        {
            get => _studentPhoto;
            set
            {
                _studentPhoto = value;
                OnPropertyChanged(nameof(StudentPhoto));
            }
        }

        public ICommand DoRegister
        {
            get => _doRegister;
            set => _doRegister = value;
        }

        public ICommand DoUnRegister
        {
            get => _doUnRegister;
            set => _doUnRegister = value;
        }

        public bool IsAutoRegistrationEnabled { get; set; }
    }
}
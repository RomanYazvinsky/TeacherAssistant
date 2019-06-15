using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dao;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.RegistrationPage
{
    public class RegistrationPageModel : AbstractModel
    {
        private ISerialUtil SerialUtil { get; }
        private IPhotoService PhotoService { get; }
        private List<StudentLessonModel> _studentLessonModels;
        private LessonModel _currentLesson;
        private StudentModel _selectedStudent;
        private StudentModel _lessonStudentsSelectedItem;
        private BitmapImage _studentPhoto;
        private string _activeStudentInfo;
        private bool _isUidAccepted = false;

        private ObservableCollection<StudentLessonModel> _registeredStudents =
            new ObservableCollection<StudentLessonModel>();

        private ObservableCollection<StudentLessonModel> _lessonStudents =
            new ObservableCollection<StudentLessonModel>();

        private ICommand _doRegister;

        private ICommand _doUnRegister;
        private StudentModel _registeredStudentsSelectedItem;
        private bool _isAutoRegistrationEnabled;
        private string _lessonStudentsFilterText = "";
        private string _registeredStudentsFilterText = "";
        private ListCollectionView _lessonStudentsView = new ListCollectionView(new List<StudentLessonModel>());
        private ListCollectionView _registeredStudentsView = new ListCollectionView(new List<StudentLessonModel>());
     
        public RegistrationPageModel(ISerialUtil serialUtil, IPhotoService photoService)
        {
            SerialUtil = serialUtil;
            PhotoService = photoService;
        }

        private List<StudentLessonModel> GetStudentMissedLessons(StudentModel model)
        {
            var now = DateTime.Now.Date;
            var studentLessonStreamModels = new List<StudentLessonModel>(
                from x in GeneralDbContext.Instance.StudentLessonModels.Include(lessonModel => lessonModel.Lesson)
                where x.student_id == model.id
                      && ((x.registered.HasValue
                           && x.registered.Value == 0) || !x.registered.HasValue)
                      && x.Lesson.stream_id == _currentLesson.stream_id
                select x);
            if (studentLessonStreamModels.Count == 0) return studentLessonStreamModels;
            studentLessonStreamModels =
                new List<StudentLessonModel>(
                    studentLessonStreamModels.Where(lessonModel =>
                        lessonModel.Lesson.Date < now));
            return studentLessonStreamModels;
        }
        
        

        private List<StudentLessonModel> GetStudentLessonModels(LessonModel lessonModel)
        {
            return new List<StudentLessonModel>(
                from studentLessonModel in GeneralDbContext.Instance.StudentLessonModels
                where studentLessonModel.lesson_id == lessonModel.id
                select studentLessonModel);
        }

        /// <summary>
        /// Compares the current list of student lesson entities (group or whole stream) with already created
        /// list and creates if some where added
        /// </summary>
        /// <param name="studentLessonModels">already created list of student lesson entities</param>
        /// <param name="lessonModel">lesson</param> 
        private void AddMissingStudents(List<StudentLessonModel> studentLessonModels, LessonModel lessonModel)
        {
            List<StudentModel> students;
            if (lessonModel.group_id == null)
            {
                students = new List<StudentModel>(
                    GeneralDbContext.Instance.StreamModels.Include(model => model.Groups)
                        .First(model => lessonModel.stream_id.Value == model.id)?.Groups.Aggregate(
                            new List<StudentModel>(), (list, model) =>
                            {
                                list.AddRange(model.Students);
                                return list;
                            }) ?? new List<StudentModel>());
            }
            else
            {
                students = new List<StudentModel>(GeneralDbContext.Instance.GroupModels
                                                      .Find(lessonModel.group_id)?.Students ??
                                                  new List<StudentModel>());
            }

            var newStudentLessonModels = students
                .Where(studentModel =>
                    studentLessonModels.All(model => model.student_id != studentModel.id)).Select(model =>
                    new StudentLessonModel
                    {
                        lesson_id = lessonModel.id,
                        student_id = model.id,
                        registered = 0
                    });
            if (students.Count == 0) return;
            var lessonModels = newStudentLessonModels as StudentLessonModel[] ?? newStudentLessonModels.ToArray();
            GeneralDbContext.Instance.StudentLessonModels.AddRange(lessonModels);
            GeneralDbContext.Instance.SaveChanges();
            studentLessonModels.AddRange(lessonModels);
        }

        private void ReadStudentData(StudentCard readData)
        {
            var student = _lessonStudents
                .FirstOrDefault(studentModel => studentModel.Student.card_uid.Equals(readData.CardUid));
            if (student != null)
            {
                UpdatePhoto(student.Student);
                UpdateDescription(student.Student);
                if (IsAutoRegistrationEnabled)
                {
                    Application.Current.Dispatcher.Invoke(() => { Register(new List<StudentLessonModel> { student }); });
                }

                return;
            }

            var studentFromDatabase = GeneralDbContext.Instance.StudentModels
                .FirstOrDefault(model => model.card_uid.Equals(readData.CardUid));
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

            var unknownStudent = new StudentModel
            {
                card_uid = readData.CardUid,
                card_id = readData.CardId,
                first_name = readData.FirstName,
                last_name = readData.LastName,
                patronymic = readData.SecondName
            };
            if (readData.FullName != null)
            {
                UpdateDescription(unknownStudent);

            }

            UpdatePhoto(unknownStudent);
        }

        private void Register(List<StudentLessonModel> models)
        {
            foreach (var studentLessonModel in models)
            {
                studentLessonModel.registered = 1;
                LessonStudentsView.Remove(studentLessonModel);
                if (SelectedLessonStudents.Contains(studentLessonModel))
                {
                    SelectedLessonStudents.Remove(studentLessonModel);
                }

                _registeredStudents.Add(studentLessonModel);
            }

            GeneralDbContext.Instance.SaveChanges();
        }

        private void RegisterExtStudent(StudentModel studentModel)
        {
            StudentLessonModel studentLessonModel = new StudentLessonModel
            {
                lesson_id = _currentLesson.id,
                student_id = studentModel.id,
                registered = 1
            };
            _registeredStudents.Add(studentLessonModel);
            GeneralDbContext.Instance.StudentLessonModels.Add(studentLessonModel);
            GeneralDbContext.Instance.SaveChanges();
        }

        private void UnRegister(List<StudentLessonModel> models)
        {
            foreach (var studentLessonModel in models)
            {
                studentLessonModel.registered = 0;
                RegisteredStudentsView.Remove(studentLessonModel);
                if (SelectedRegisteredStudents.Contains(studentLessonModel))
                {
                    SelectedRegisteredStudents.Remove(studentLessonModel);
                }

                _lessonStudents.Add(studentLessonModel);
            }

            GeneralDbContext.Instance.SaveChanges();
        }

        private void UpdatePhoto(StudentModel model)
        {
            if (model == null) return;
            StudentPhoto = null;
            // Запускаем асинхронную задачу
            Task.Run(async () =>
            {
                // Скачиваем фото студента и получаем путь к файлу
                string photoPath = await PhotoService.DownloadPhoto(model.card_id);
                if (photoPath == null)
                {
                    return;
                }

                // Если путь вернулся успешно, то мы можем считать загруженный файл в объект изображения
                BitmapImage image = await PhotoService.GetImage(photoPath);
                // Возвращаемся в основной поток - тот при первой же возможности отобразит изображение в программе
                Application.Current.Dispatcher.Invoke(() => { StudentPhoto = image; },
                    DispatcherPriority.DataBind);
            });
        }

        private void UpdateDescription(StudentModel model)
        {
            if (model == null) return;
            var missedLessons = GetStudentMissedLessons(model);
            var missedLectures = missedLessons.Where(lessonModel =>
                lessonModel.Lesson.type_id.HasValue && lessonModel.Lesson.type_id.Value == (long)LessonType.Lecture);
            var missedPractices = missedLessons.Where(lessonModel =>
                lessonModel.Lesson.type_id.HasValue && lessonModel.Lesson.type_id.Value == (long)LessonType.Practice);
            var missedLabs = missedLessons.Where(lessonModel =>
                lessonModel.Lesson.type_id.HasValue &&
                lessonModel.Lesson.type_id.Value == (long)LessonType.Laboratory);
            ActiveStudentInfo =
                $"{model.last_name} {model.first_name} {(model.patronymic ?? "")} \n" +
                $"Пропуски: {missedLessons.Count} -> Л {missedLectures.Count()} | " +
                $"П {missedPractices.Count()} | Лб {missedLabs.Count()}";
        }

        public void Remove(StudentLessonModel model)
        {
            if (model == null)
            {
                return;
            }

            LessonStudentsView.Remove(model);
            GeneralDbContext.Instance.StudentLessonModels.Remove(model);
            GeneralDbContext.Instance.SaveChanges();
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

        public ListCollectionView RegisteredStudentsView
        {
            get => _registeredStudentsView;
            set
            {
                _registeredStudentsView = value;
                OnPropertyChanged(nameof(RegisteredStudentsView));
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


        public ListCollectionView LessonStudentsView
        {
            get => _lessonStudentsView;
            set
            {
                _lessonStudentsView = value;
                OnPropertyChanged(nameof(LessonStudentsView));
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

        public bool IsAutoRegistrationEnabled
        {
            get => _isAutoRegistrationEnabled;
            set
            {
                _isAutoRegistrationEnabled = value;
                OnPropertyChanged(nameof(IsAutoRegistrationEnabled));
            }
        }

        public string LessonStudentsFilterText
        {
            get => _lessonStudentsFilterText;
            set
            {
                _lessonStudentsFilterText = value.ToLowerInvariant();
                OnPropertyChanged(nameof(LessonStudentsFilterText));
                if (_lessonStudentsFilterText != "")
                {
                    LessonStudentsView.Filter = o =>
                    {
                        var student = ((StudentLessonModel)o).Student;
                        return student.first_name != null &&
                               student.first_name.ToLowerInvariant().Contains(_lessonStudentsFilterText)
                               || student.last_name != null && student.last_name.ToLowerInvariant()
                                   .Contains(_lessonStudentsFilterText)
                               || student.patronymic != null && student.patronymic.ToLowerInvariant()
                                   .Contains(_lessonStudentsFilterText);
                    };
                }
                else
                {
                    LessonStudentsView.Filter = null;
                }
            }
        }

        public string RegisteredStudentsFilterText
        {
            get => _registeredStudentsFilterText;
            set
            {
                _registeredStudentsFilterText = value.ToLowerInvariant();
                OnPropertyChanged(nameof(RegisteredStudentsFilterText));
                if (_registeredStudentsFilterText != "")
                {
                    RegisteredStudentsView.Filter = o =>
                    {
                        var student = ((StudentLessonModel)o).Student;
                        return (student.first_name != null && student.first_name.ToLowerInvariant()
                                    .Contains(_registeredStudentsFilterText))
                               || (student.last_name != null && student.last_name.ToLowerInvariant()
                                       .Contains(_registeredStudentsFilterText))
                               || (student.patronymic != null && student.patronymic.ToLowerInvariant()
                                       .Contains(_registeredStudentsFilterText));
                    };
                }
                else
                {
                    RegisteredStudentsView.Filter = null;
                }
            }
        }

        public override async Task Init(string id)
        {
            _doRegister = new CommandHandler(() =>
            {
                Register(new List<StudentLessonModel>(SelectedLessonStudents));
            });
            _doUnRegister = new CommandHandler(() =>
            {
                UnRegister(new List<StudentLessonModel>(SelectedRegisteredStudents));
            });
            SimpleSubscribe<LessonModel>(id + "." + SchedulePageModel.SELECTED_LESSON, model =>
            {
                if (model == null) return;
                _currentLesson = model;
                _lessonStudents.Clear();
                _registeredStudents.Clear();
                var studentModels = GetStudentLessonModels(model);
                AddMissingStudents(studentModels, _currentLesson);
                _studentLessonModels = studentModels;
                _lessonStudents =
                    new ObservableCollection<StudentLessonModel>(
                        _studentLessonModels.Where(lessonModel =>
                            !lessonModel.registered.HasValue || lessonModel.registered.Value == 0));
                _registeredStudents = new ObservableCollection<StudentLessonModel>(
                    _studentLessonModels.Where(lessonModel =>
                        lessonModel.registered.HasValue && lessonModel.registered.Value != 0));
                LessonStudentsView = new ListCollectionView(_lessonStudents);
                RegisteredStudentsView = new ListCollectionView(_registeredStudents);
            });
            SimpleSubscribe<StudentModel>("LastReadStudentCard", model => SelectedStudent = model);

            SerialUtil.OnRead().Subscribe(ReadStudentData);
        }
    }
}
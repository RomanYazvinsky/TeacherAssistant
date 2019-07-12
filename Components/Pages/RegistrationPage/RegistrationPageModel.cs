using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Pages.CommonStudentLessonViewPage;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;

namespace TeacherAssistant.RegistrationPage {
    public class RegistrationPageModel : AbstractModel {
        private static readonly string LocalizationKey = "page.registration";
        private LessonModel _currentLesson;
        private StudentCardService StudentCardService { get; }
        private IPhotoService PhotoService { get; }
        private List<StudentLessonModel> _studentLessons = new List<StudentLessonModel>();
        private List<StudentLessonModel> _internalStudentLessons = new List<StudentLessonModel>();
        private DispatcherTimer _timerToEnd;
        private bool _isDragSourceRegisteredStudents;
        private bool _isDragSourceLessonStudents;

        public RegistrationPageModel(
            string id,
            StudentCardService studentCardService,
            IPhotoService photoService
        ) : base(id) {
            this.StudentCardService = studentCardService;
            this.PhotoService = photoService;
            this.DoRegister = new CommandHandler(Register);
            this.DoUnRegister = new CommandHandler(UnRegister);
//            this.DragStartLessonStudents = () => _isDragSourceLessonStudents = true;
//            this.DragStartRegisteredStudents = () => _isDragSourceRegisteredStudents = true;
            this.ShowStudent = new CommandHandler(() => {
                var studentLessonModel = this.SelectedStudent;
                var openPage = this.PageService.OpenPage(new PageProperties {
                    Header = studentLessonModel.Student.LastName,
                    PageType = typeof(StudentViewPage.StudentViewPage)
                }, this.Id);
                StoreManager.Publish(studentLessonModel.Student, openPage, "Student");
            });
            this.LessonStudentsTableDropAvailability =
                data => !_isDragSourceLessonStudents && data.Data.Count > 0 && data.Data[0] is StudentLessonModel;
            this.RegisteredDropAvailability =
                dragData => !_isDragSourceRegisteredStudents &&
                            dragData.Data.Count > 0 &&
                            (IsDragFromStudentTable(dragData.Data) ||
                             IsDragFromRegistrationTable(dragData.Data));
            this.DropOnRegisteredStudents = dragData => {
                if (IsDragFromRegistrationTable(dragData.Data)) {
                    Register();
                }

                if (IsDragFromStudentTable(dragData.Data)) {
                    AcceptDropFromStudentTable(dragData.Data);
                }

                dragData.Accept();
                _isDragSourceLessonStudents = false;
                _isDragSourceRegisteredStudents = false;
            };
            this.DropOnLessonStudents = dragData => {
                UnRegister();
                dragData.Accept();
                _isDragSourceLessonStudents = false;
                _isDragSourceRegisteredStudents = false;
            };
            this.WhenAnyValue(model => model.SelectedStudent).Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe
                (
                    student => {
                        UpdatePhoto(student?.Student);
                        UpdateDescription(student?.Student);
                    }
                );
            this.WhenAnyValue(model => model.TimerState).Where(NotNull).Subscribe(state => {
                this.TimerString = $"{state.TimeLeft:hh\\:mm\\:ss}/{state.CurrentTime:HH:mm:ss}";
            });
            this.StudentCardService.ReadStudentCard.Subscribe(ReadStudentData);
        }

        private static bool IsDragFromStudentTable(List<object> list) {
            return list[0] is StudentModel;
        }

        private static bool IsDragFromRegistrationTable(List<object> list) {
            return list[0] is StudentLessonModel;
        }

        private void AcceptDropFromStudentTable(List<object> list) {
            var studentModels = list.Cast<StudentModel>().ToList();
            var studentIds = studentModels.Select(model => model.Id);
            var studentLessonsThatAlreadyProcessed
                = _db.StudentLessonModels
                    .Where(model => model._LessonId == _currentLesson.Id
                                    && studentIds.Any(l => model._StudentId == l))
                    .ToList();
            studentLessonsThatAlreadyProcessed.ForEach(model => {
                if (model.IsLessonMissed) {
                    Register(model);
                }
            });
            var externalStudentsToRegister = studentModels
                .Where(model => studentLessonsThatAlreadyProcessed
                    .All(lessonModel => lessonModel._StudentId != model.Id)
                );
            foreach (var studentModel in externalStudentsToRegister) {
                RegisterExtStudent(studentModel);
            }
        }

        public Func<DragData, bool> RegisteredDropAvailability { get; set; }
        public Func<DragData, bool> LessonStudentsTableDropAvailability { get; set; }
        public Action<DragData> DropOnRegisteredStudents { get; set; }
        public Action<DragData> DropOnLessonStudents { get; set; }
        public Action DragStartRegisteredStudents { get; set; }
        public Action DragStartLessonStudents { get; set; }

        public ObservableRangeCollection<object> RegisteredStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedRegisteredStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedLessonStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> LessonStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public Dictionary<string, ListSortDirection> RegisteredStudentsSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {nameof(StudentLessonModel.RegistrationTime), ListSortDirection.Descending},
                {"Student.LastName", ListSortDirection.Ascending},
                {"Student.FirstName", ListSortDirection.Ascending}
            };

        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection> {
            {"Student.LastName", ListSortDirection.Ascending},
            {"Student.FirstName", ListSortDirection.Ascending}
        };

        [Reactive] public StudentLessonModel SelectedStudent { get; set; }

        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public string StudentDescription { get; set; }

        [Reactive] public ICommand DoRegister { get; set; }

        [Reactive] public ICommand DoUnRegister { get; set; }
        [Reactive] public ICommand ShowStudent { get; set; }

        [Reactive] public bool IsAutoRegistrationEnabled { get; set; }

        [Reactive] public Visibility ActiveStudentInfoVisibility { get; set; } = Visibility.Hidden;

        [Reactive] public TimerState TimerState { get; set; }
        [Reactive] public string TimerString { get; set; }


        public override List<ButtonConfig> GetControls() {
            var buttonConfigs = base.GetControls();
            buttonConfigs.Add(new ButtonConfig {
                Command = new CommandHandler(() => {
                    var openPage = this.PageService.OpenPage(new PageProperties {
                        PageType = typeof(CommonStudentLessonViewPage),
                        Header = _currentLesson.Group == null
                            ? _currentLesson.Stream.Name
                            : _currentLesson.Group.Name + " " +
                              Localization["common.lesson.type." + _currentLesson.LessonType] + " " +
                              _currentLesson.Date?.ToString("dd.MM")
                    }, this.Id);
                    StoreManager.Publish(_currentLesson, openPage, "Lesson");
                }),
                Text = "Занятие 1"
            });
            return buttonConfigs;
        }

        public Func<object, string, bool> Filter { get; set; } = (o, s) => {
            s = s.ToLowerInvariant();
            var student = ((StudentLessonModel) o).Student;
            return student.FirstName != null
                   && student.FirstName.ToLowerInvariant()
                       .Contains(s)
                   || student.LastName != null
                   && student.LastName.ToLowerInvariant()
                       .Contains(s)
                   || student.SecondName != null
                   && student.SecondName.ToLowerInvariant()
                       .Contains(s);
        };

        /// <summary>
        ///     Compares the current list of student lesson entities (group or whole stream) with already created
        ///     list and creates if some where added
        /// </summary>
        /// <param name="studentLessonModels">already created list of student lesson entities</param>
        /// <param name="lessonModel">lesson</param>
        private void AddMissingStudents(List<StudentLessonModel> studentLessonModels, LessonModel lessonModel) {
            List<StudentModel> students;
            if (lessonModel.Group == null)
                students = lessonModel.Stream.Groups.Aggregate
                (
                    new List<StudentModel>(),
                    (list, model) => {
                        list.AddRange(model.Students);
                        return list;
                    }
                );
            else {
                students = lessonModel.Group.Students.ToList();
            }

            var newStudentLessonModels = students
                .Where
                (
                    studentModel =>
                        studentLessonModels.All
                        (
                            model => (model.Student?.Id) != studentModel.Id
                        )
                )
                .Select
                (
                    model =>
                        new StudentLessonModel {
                            Lesson = lessonModel,
                            Student = model,
                            IsRegistered = false
                        }
                )
                .ToList();
            if (students.Count == 0)
                return;
            _db.StudentLessonModels.AddRange(newStudentLessonModels);
            _db.SaveChangesAsync();
            studentLessonModels.AddRange(newStudentLessonModels);
        }

        private void ReadStudentData(StudentCard readData) {
            var student = this.LessonStudents.Cast<StudentLessonModel>()
                .FirstOrDefault(studentModel => studentModel.Student.CardUid.Equals(readData.CardUid));
            if (student != null) {
                UpdatePhoto(student.Student);
                UpdateDescription(student.Student);
                if (this.IsAutoRegistrationEnabled)
                    UpdateFromAsync(() => Register(student));
                return;
            }

            var studentFromDatabase = _db.StudentModels.FirstOrDefault(model => model.CardUid.Equals(readData.CardUid));
            if (studentFromDatabase != null) {
                var studentView = new StudentModel();
                UpdatePhoto(studentView);
                UpdateDescription(studentView);
                if (this.IsAutoRegistrationEnabled)
                    UpdateFromAsync(() => { RegisterExtStudent(studentView); });

                return;
            }

            var unknownStudent =
                new StudentModel {
                    CardUid = readData.CardUid,
                    FirstName = readData.FirstName,
                    LastName = readData.LastName,
                    SecondName = readData.SecondName
                };
            if (readData.FullName != null)
                UpdateDescription(unknownStudent);

            UpdatePhoto(unknownStudent);
        }

        private void Register() {
            var studentLessonModels = this.SelectedLessonStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonModel studentLessonModel in studentLessonModels) {
                studentLessonModel.IsRegistered = true;
                studentLessonModel.RegistrationTime = DateTime.Now;
                this.LessonStudents.Remove(studentLessonModel);
                this.RegisteredStudents.Add(studentLessonModel);
            }

            _db.SaveChangesAsync();
        }

        private void Register(StudentLessonModel model) {
            model.IsRegistered = true;
            model.RegistrationTime = DateTime.Now;
            this.LessonStudents.Remove(model);
            this.RegisteredStudents.Add(model);

            _db.ThrottleSave();
        }

        private void RegisterExtStudent(StudentModel studentModel) {
            var studentLessonModel = new StudentLessonModel {
                Lesson = _currentLesson,
                Student = studentModel,
                IsRegistered = true,
                RegistrationTime = DateTime.Now
            };
            this.RegisteredStudents.Add(studentLessonModel);
            _db.StudentLessonModels.Add(studentLessonModel);
            _db.ThrottleSave();
        }

        private void UnRegister() {
            var studentModels = this.SelectedRegisteredStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonModel studentLessonModel in studentModels) {
                if (!_internalStudentLessons.Contains(studentLessonModel)) {
                    this.RegisteredStudents.Remove(studentLessonModel);
                    _db.StudentLessonModels.Remove(studentLessonModel);
                    continue;
                }

                studentLessonModel.IsRegistered = false;
                studentLessonModel.RegistrationTime = null;
                this.RegisteredStudents.Remove(studentLessonModel);
                this.LessonStudents.Add(studentLessonModel);
            }

            _db.SaveChanges();
        }

        private void UpdatePhoto(StudentModel model) {
            if (model == null) {
                UpdateFromAsync(() => this.StudentPhoto = null);
                return;
            }

            Task.Run
            (
                async () => {
                    try {
                        var photoPath = await this.PhotoService.DownloadPhoto(StudentModel.CardUidToId(model.CardUid));
                        if (photoPath == null)
                            return;

                        var image = this.PhotoService.GetImage(photoPath);

                        UpdateFromAsync(() => { this.StudentPhoto = image; });
                    }
                    catch (Exception e) {
                        Debug.WriteLine(e);
                        throw;
                    }
                }
            );
        }

        private void UpdateDescription(StudentModel model) {
            if (model == null) {
                this.ActiveStudentInfoVisibility = Visibility.Collapsed;
                return;
            }

            var now = DateTime.Now;
            var missedLessons = _db.GetStudentMissedLessons(model, _currentLesson._Stream, now);
            var missedLectures = missedLessons.Where
            (
                lessonModel =>
                    lessonModel.Lesson._TypeId.HasValue && lessonModel.Lesson.LessonType == LessonType.Lecture
            );
            var missedPractices = missedLessons
                .Where
                (
                    lessonModel => lessonModel.Lesson._TypeId.HasValue
                                   && lessonModel.Lesson.LessonType == LessonType.Practice
                );
            var missedLabs = missedLessons
                .Where
                (
                    lessonModel => lessonModel.Lesson._TypeId.HasValue
                                   && lessonModel.Lesson.LessonType == LessonType.Laboratory
                );
            this.ActiveStudentInfoVisibility = Visibility.Visible;
            this.StudentDescription = string.Format(Localization["page.registration.active.student.info"],
                model.LastName,
                model.FirstName,
                model.SecondName,
                missedLessons.Count,
                missedLectures.Count(),
                missedPractices.Count(),
                missedLabs.Count()
            );
        }

        public void Remove(StudentLessonModel model) {
            if (model == null)
                return;

            this.LessonStudents.Remove(model);
            _db.StudentLessonModels.Remove(model);
            _db.SaveChanges();
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void StartTimer(LessonModel model) {
            TimeSpan timeLeft;
            var now = DateTime.Now;
            if (model.Date.HasValue && model.Schedule.End.HasValue) {
                timeLeft = model.Date.Value.Date + model.Schedule.End.Value - now;
            }
            else {
                timeLeft = TimeSpan.Zero;
            }

            this.TimerState = new TimerState {
                TimeLeft = timeLeft,
                CurrentTime = now
            };
            var timerToEnd = new DispatcherTimer();
            timerToEnd.Tick += (sender, args) => {
                var now1 = DateTime.Now;

                var timeLeft1 = this.TimerState.TimeLeft - TimeSpan.FromMilliseconds(1000);
                if (timeLeft1.TotalMilliseconds <= 0) {
                    this.TimerState = new TimerState {
                        TimeLeft = TimeSpan.Zero,
                        CurrentTime = now1
                    };
                    return;
                }

                this.TimerState = new TimerState {
                    TimeLeft = timeLeft1,
                    CurrentTime = now1
                };
            };
            timerToEnd.Interval = TimeSpan.FromMilliseconds(1000);
            timerToEnd.Start();
            _timerToEnd = timerToEnd;
        }

        private void StopTimer() {
            _timerToEnd?.Stop();
        }

        public override void Dispose() {
            base.Dispose();
            StopTimer();
        }

        public override Task Init() {
            Select<LessonModel>(this.Id, SchedulePageModel.SelectedLessonKey)
                .Subscribe
                (
                    lesson => {
                        if (lesson == null)
                            return;
                        _currentLesson = lesson;
                        StopTimer();
                        this.LessonStudents.Clear();
                        this.RegisteredStudents.Clear();
                        var loadedStudentLessons = lesson.StudentLessons.ToList();
                        AddMissingStudents(loadedStudentLessons, _currentLesson);
                        _studentLessons = loadedStudentLessons;
                        var notRegisteredStudentLessons =
                            _studentLessons.Where(lessonModel => !(lessonModel.IsRegistered ?? false)).ToList();
                        this.LessonStudents.AddRange(notRegisteredStudentLessons);

                        var registeredStudentLesson =
                            _studentLessons.Where(lessonModel => lessonModel.IsRegistered == true).ToList();
                        this.RegisteredStudents.AddRange(registeredStudentLesson);
                        _internalStudentLessons = FindInternalStudents(_studentLessons, lesson);
                        StartTimer(_currentLesson);
                    }
                );

            return Task.CompletedTask;
        }

        private List<StudentLessonModel> FindInternalStudents(List<StudentLessonModel> allStudentLessons,
            LessonModel lesson) {
            var internalStudents = lesson.Group?.Students ?? lesson.Stream.Students;

            return allStudentLessons.Where(sl => internalStudents.Any(student => student.Id == sl._StudentId)).ToList();
        }
    }

    public class TimerState {
        public DateTime CurrentTime { get; set; }
        public TimeSpan TimeLeft { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Containers;
using DynamicData;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Dao.ViewModels;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.Pages;
using TeacherAssistant.Pages.CommonStudentLessonViewPage;
using TeacherAssistant.Pages.StudentTablePage.ViewModels;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.StudentViewPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.RegistrationPage {
    public class RegistrationPageModel : AbstractModel<RegistrationPageModel> {
        private readonly TabPageHost _tabPageHost;
        private readonly WindowPageHost _windowPageHost;
        private readonly LocalDbContext _db;
        private static readonly string LocalizationKey = "page.registration";
        private StudentCardService StudentCardService { get; }
        private PhotoService PhotoService { get; }
        private List<StudentLessonInfoViewModel> _studentLessons = new List<StudentLessonInfoViewModel>();
        private List<StudentLessonInfoViewModel> _internalStudentLessons = new List<StudentLessonInfoViewModel>();
        private IDisposable _timerToEnd;
        private StudentEntity _selectedStudent;
        private StudentLessonInfoViewModel _selectedStudentLesson;


        public RegistrationPageModel(
            StudentCardService studentCardService,
            PhotoService photoService,
            TabPageHost tabPageHost,
            WindowPageHost windowPageHost,
            LocalDbContext db,
            RegistrationPageToken token,
            MainReducer mainReducer,
            PageControllerReducer reducer
        ) {
            _tabPageHost = tabPageHost;
            _windowPageHost = windowPageHost;
            _db = db;
            this.StudentCardService = studentCardService;
            this.PhotoService = photoService;
            this.DoRegister = ReactiveCommand.Create(() => {
                if (this.AllStudentsMode) {
                    foreach (IStudentViewModel selectedStudent in this.SelectedStudents.ToList()) {
                        RegisterExtStudent(selectedStudent.Student);
                    }
                }
                else {
                    Register();
                }
            });
            this.DoUnRegister = ReactiveCommand.Create(UnRegister);

            this.ShowStudent = ReactiveCommand.Create(() => {
                var selectedStudent = _selectedStudent;
                var studentViewPageToken = new StudentViewPageToken("Student", selectedStudent);
                tabPageHost.AddPageAsync<StudentViewPageModule, StudentViewPageToken>(studentViewPageToken);
            });
            this.AddStudentNote = ReactiveCommand.Create(() => {
                var noteFormToken = new NoteListFormToken("Заметки", () => new StudentLessonNote() {
                    StudentLesson = _selectedStudentLesson.StudentLesson,
                    EntityId = _selectedStudentLesson.StudentLesson.Id
                }, _selectedStudentLesson.StudentLesson.Notes);
                windowPageHost.AddPageAsync<NoteListFormModule, NoteListFormToken>(noteFormToken);
            });
            this.ToggleAllStudentTable = new ButtonConfig {
                Command = ReactiveCommand.Create(() => this.AllStudentsMode = !this.AllStudentsMode),
                Text = Localization["Все студенты"]
            };
            this.AllStudentsFilter = (o, s) => {
                var student = ((IStudentViewModel) o).Student;
                var alreadyAdded =
                    IsStudentAlreadyRegistered(RegisteredStudents.Cast<StudentLessonInfoViewModel>(), student)
                    || IsStudentAlreadyRegistered(LessonStudents.Cast<StudentLessonInfoViewModel>(), student);
                if (alreadyAdded) return false;
                s = s.ToLowerInvariant();
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
            this.WhenActivated(disposable => {
                this.WhenAnyValue(model => model.TimerState)
                    .Where(LambdaHelper.NotNull)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(state => {
                        this.TimerString = $"{state.TimeLeft:hh\\:mm\\:ss}/{state.CurrentTime:HH:mm:ss}";
                    }).DisposeWith(disposable);
                this.WhenAnyValue(model => model.Lesson)
                    .Where(LambdaHelper.NotNull)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(entity => {
                        var groupName = entity.Group?.Name ?? entity.Stream.Name;
                        var lessonInfo =
                            $"{Localization["common.lesson.type." + entity.LessonType]}: {entity.Order}/{entity.GetLessonsCount()}";
                        this.LessonInfoState = new LessonInfoState {
                            GroupName = groupName,
                            LessonInfo = lessonInfo,
                            Date = entity.Date?.ToString("dd.MM.yyyy"),
                            Time =
                                $"[{entity.Schedule.OrderNumber}] {entity.Schedule.Begin:hh\\:mm} - {entity.Schedule.End:hh\\:mm}"
                        };
                    }).DisposeWith(disposable);
                this.WhenAnyValue(model => model.AllStudentsMode)
                    .Throttle(TimeSpan.FromMilliseconds(100))
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(b => {
                        this.ToggleAllStudentTable.Text = b ? Localization["Занятие"] : Localization["Все студенты"];
                        if (!b || this.AllStudents.Count != 0) return;
                        var studentViewModels = db.Students
                            .Include(model => model.Groups)
                            .ToList() // create query and load
                            .Select(model => new StudentViewModel(model))
                            .ToList();
                        this.AllStudents.AddRange(studentViewModels);
                    }).DisposeWith(disposable);

                this.WhenAnyValue(model => model.IsLessonChecked)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(b => {
                        if (this.Lesson == null) {
                            return;
                        }

                        this.Lesson.Checked = b;
                        this._db.ThrottleSave();
                    }).DisposeWith(disposable);
                this.LessonStudentsTableConfig.SelectedItem
                    .Where(LambdaHelper.NotNull)
                    .Merge(this.RegisteredStudentsTableConfig.SelectedItem.AsObservable().Where(LambdaHelper.NotNull))
                    .Cast<StudentLessonInfoViewModel>()
                    .Do(o => this._selectedStudentLesson = o)
                    .Select(o => o.StudentLesson.Student)
                    .Merge(this.AllStudentsTableConfig.SelectedItem.Where(LambdaHelper.NotNull))
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe
                    (
                        o => {
                            var studentEntity = o as StudentEntity;
                            _selectedStudent = studentEntity;
                            UpdateDescription(studentEntity);
                        }
                    ).DisposeWith(disposable);

                this.WhenRemoved<LessonEntity>()
                    .Where(entities => entities.Any(entity => entity.Id == this.Lesson?.Id))
                    .Subscribe(_ => token.Deactivate())
                    .DisposeWith(disposable);
                this.StudentCardService.ReadStudentCard.Subscribe(ReadStudentData);
            });
            this.AllStudentsTableConfig = new TableConfig {
                Sorts = Sorts,
                Filter = this.AllStudentsFilter,
                DragConfig = new DragConfig {
                    SourceId = token.Id + nameof(this.AllStudentsTableConfig),
                    SourceType = nameof(StudentEntity),
                    DragValuePath = "Student"
                },
                IsFilterDependsOnlyOnFilterValue = false
            };
            this.RegisteredStudentsTableConfig = new TableConfig {
                Sorts = RegisteredStudentsSorts,
                Filter = this.Filter,
                DragConfig = new DragConfig {
                    SourceId = token.Id + nameof(this.RegisteredStudentsTableConfig),
                    SourceType = nameof(StudentLessonEntity),
                    DropAvailability = data => data.Data.Count > 0 &&
                                               ((data.SenderType.Equals(nameof(StudentLessonEntity))
                                                 && data.SenderId.Equals(this.LessonStudentsTableConfig.DragConfig
                                                     .SourceId)
                                                )
                                                || data.SenderType.Equals(nameof(StudentEntity))),
                    Drop = async () => {
                        var dragData = await mainReducer.Select(state => state.DragData).FirstOrDefaultAsync();
                        if (dragData == null) {
                            return;
                        }

                        if (nameof(StudentLessonEntity).Equals(dragData.SenderType)) {
                            RunInUiThread(Register);
                        }

                        if (nameof(StudentEntity).Equals(dragData.SenderType)) {
                            AcceptDropFromStudentTable(dragData.Data);
                        }

                        dragData.Accept();
                    },
                    DragStart = data => mainReducer.DispatchSetValueAction(state => state.DragData, data)
                }
            };
            this.LessonStudentsTableConfig = new TableConfig {
                Sorts = Sorts,
                Filter = this.Filter,
                DragConfig = new DragConfig {
                    SourceId = token.Id + nameof(this.LessonStudentsTableConfig),
                    SourceType = nameof(StudentLessonEntity),
                    DropAvailability = data => data.Data.Count > 0
                                               && data.SenderId.Equals(this.RegisteredStudentsTableConfig.DragConfig
                                                   .SourceId),
                    Drop = async () => {
                        var dragData = await mainReducer.Select(state => state.DragData).FirstAsync();
                        RunInUiThread(UnRegister);
                        dragData.Accept();
                    },
                    DragStart = data => mainReducer.DispatchSetValueAction(state => state.DragData, data)
                }
            };
            Init(token.Lesson);
            reducer.Dispatch(new RegisterControlsAction(token, GetControls()));
        }

        private async void Init(LessonEntity lesson) {
            if (lesson == null)
                return;
            this.Lesson = await _db.Lessons.FindAsync(lesson.Id) ?? lesson.Clone();
            StopTimer();
            this.LessonStudents.Clear();
            this.RegisteredStudents.Clear();
            var loadedStudentLessons = lesson.StudentLessons?.ToList() ?? new List<StudentLessonEntity>();
            AddMissingStudents(loadedStudentLessons, this.Lesson);
            _studentLessons = loadedStudentLessons
                .Select(entity => new StudentLessonInfoViewModel(entity, AddStudentNote, ShowStudent)).ToList();
            var notRegisteredStudentLessons =
                _studentLessons.Where(lessonModel => !(lessonModel.StudentLesson.IsRegistered ?? false)).ToList();
            this.LessonStudents.AddRange(notRegisteredStudentLessons);

            var registeredStudentLesson =
                _studentLessons.Where(lessonModel => lessonModel.StudentLesson.IsRegistered == true).ToList();
            this.RegisteredStudents.AddRange(registeredStudentLesson);
            _internalStudentLessons = FindInternalStudents(_studentLessons, lesson);
            this.IsLessonChecked = lesson.Checked;
            UpdateRegistrationInfo();
            StartTimer(this.Lesson);
        }

        private void AcceptDropFromStudentTable(List<object> list) {
            var studentModels = list.Cast<StudentEntity>().ToList();
            var studentIds = studentModels.Select(model => model.Id);
            var studentLessonsThatAlreadyProcessed
                = _db.StudentLessons
                    .Where(model => model._LessonId == Lesson.Id
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

            UpdateRegistrationInfo();
        }

        public TableConfig RegisteredStudentsTableConfig { get; }
        public TableConfig LessonStudentsTableConfig { get; }


        private ObservableCollection<object> SelectedRegisteredStudents =>
            this.RegisteredStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> SelectedLessonStudents =>
            this.LessonStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> SelectedStudents => this.AllStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> RegisteredStudents => this.RegisteredStudentsTableConfig.TableItems;

        private ObservableCollection<object> LessonStudents => this.LessonStudentsTableConfig.TableItems;


        private ObservableCollection<object> AllStudents => this.AllStudentsTableConfig.TableItems;

        private static Dictionary<string, ListSortDirection> RegisteredStudentsSorts { get; } =
            new Dictionary<string, ListSortDirection> {
                {nameof(StudentLessonEntity.RegistrationTime), ListSortDirection.Descending},
                {"Student.LastName", ListSortDirection.Ascending},
                {"Student.FirstName", ListSortDirection.Ascending}
            };

        private static Dictionary<string, ListSortDirection> Sorts { get; } =
            new Dictionary<string, ListSortDirection> {
                {"Student.LastName", ListSortDirection.Ascending},
                {"Student.FirstName", ListSortDirection.Ascending}
            };


        private bool IsStudentAlreadyRegistered(IEnumerable<StudentLessonInfoViewModel> studentLessons,
            StudentEntity student) {
            return studentLessons.Any(studentLesson =>
                studentLesson.StudentLesson._StudentId == student.Id &&
                studentLesson.StudentLesson._LessonId == this.Lesson.Id);
        }

        [Reactive] public LessonEntity Lesson { get; set; }

        [Reactive] public LessonInfoState LessonInfoState { get; set; }
        [Reactive] public LessonRegistrationInfoState RegistrationInfoState { get; set; }

        [Reactive] public StudentDescription StudentDescription { get; set; }

        public ICommand DoRegister { get; }

        public ICommand DoUnRegister { get; }
        public ICommand ShowStudent { get; set; }
        public ICommand AddStudentNote { get; set; }
        public ButtonConfig ToggleAllStudentTable { get; }

        [Reactive] public bool IsAutoRegistrationEnabled { get; set; }
        [Reactive] public bool IsLessonChecked { get; set; }
        [Reactive] public bool AllStudentsMode { get; set; }
        public TableConfig AllStudentsTableConfig { get; }

        [Reactive] public Visibility ActiveStudentInfoVisibility { get; set; } = Visibility.Hidden;

        [Reactive] public TimerState TimerState { get; set; }
        [Reactive] public string TimerString { get; set; }


        private List<ButtonConfig> GetControls() {
            var buttonConfigs = new List<ButtonConfig> {
                GetRefreshButtonConfig()
            };
            buttonConfigs.Add(new ButtonConfig {
                Command = new CommandHandler(() => {
                    var title = this.Lesson.Group == null
                        ? this.Lesson.Stream.Name
                        : this.Lesson.Group.Name + " " +
                          Localization["common.lesson.type." + this.Lesson.LessonType] + " " +
                          this.Lesson.Date?.ToString("dd.MM");
                    var token = new TableLessonViewToken(title, this.Lesson);
                    _tabPageHost.AddPageAsync<TableLessonViewModule, TableLessonViewToken>(token);
                }),
                Text = "Занятие 1"
            });
            buttonConfigs.Add(new ButtonConfig {
                Command = ReactiveCommand.Create(() =>
                    _windowPageHost.AddPageAsync(new NoteListFormToken(
                        "Заметки",
                        () => new LessonNote {
                            Lesson = Lesson,
                            EntityId = Lesson.Id
                        },
                        Lesson.Notes
                    ))
                ),
                Text = "Заметки"
            });
            return buttonConfigs;
        }

        public Func<object, string, bool> Filter { get; set; } = (o, s) => {
            s = s.ToLowerInvariant();
            var student = ((StudentLessonInfoViewModel) o).Student;
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

        public Func<object, string, bool> AllStudentsFilter { get; set; }

        /// <summary>
        ///     Compares the current list of student lesson entities (group or whole stream) with already created
        ///     list and creates if some where added
        /// </summary>
        /// <param name="studentLessonModels">already created list of student lesson entities</param>
        /// <param name="lessonEntity">lesson</param>
        private void AddMissingStudents(List<StudentLessonEntity> studentLessonModels, LessonEntity lessonEntity) {
            List<StudentEntity> students;
            if (lessonEntity.Group == null)
                students = lessonEntity.Stream.Groups?.Aggregate
                (
                    new List<StudentEntity>(),
                    (list, model) => {
                        list.AddRange(model.Students ?? new List<StudentEntity>());
                        return list;
                    }
                );
            else {
                students = lessonEntity.Group.Students?.ToList();
            }

            students = students ?? new List<StudentEntity>();
            var newStudentLessonModels = students
                .Where
                (
                    studentModel => studentLessonModels.All(model => model.Student?.Id != studentModel.Id)
                )
                .Select
                (
                    model => new StudentLessonEntity {
                        Lesson = lessonEntity,
                        Student = model,
                        IsRegistered = false
                    }
                )
                .ToList();
            if (students.Count == 0)
                return;
            _db.StudentLessons.AddRange(newStudentLessonModels);
            _db.SaveChangesAsync();
            studentLessonModels.AddRange(newStudentLessonModels);
        }

        private void ReadStudentData(StudentCard readData) {
            var student = this.LessonStudents.Cast<StudentLessonEntity>()
                .FirstOrDefault(studentModel => studentModel.Student.CardUid.Equals(readData.CardUid));
            if (student != null) {
                UpdateDescription(student.Student);
                if (this.IsAutoRegistrationEnabled)
                    RunInUiThread(() => Register(student));
                return;
            }

            var studentFromDatabase = _db.Students.FirstOrDefault(model => model.CardUid.Equals(readData.CardUid));
            if (studentFromDatabase != null) {
                var studentEntity = studentFromDatabase.Clone();
                UpdateDescription(studentEntity);
                if (this.IsAutoRegistrationEnabled)
                    RunInUiThread(() => {
                        RegisterExtStudent(studentEntity);
                        UpdateRegistrationInfo();
                    });

                return;
            }

            var unknownStudent = new StudentEntity {
                CardUid = readData.CardUid,
                FirstName = readData.FirstName,
                LastName = readData.LastName,
                SecondName = readData.SecondName
            };
            if (readData.FullName != null) {
                UpdateDescription(unknownStudent);
            }
        }

        private void Register() {
            var studentLessonModels = this.SelectedLessonStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonInfoViewModel studentLessonModel in studentLessonModels) {
                studentLessonModel.StudentLesson.IsRegistered = true;
                studentLessonModel.StudentLesson.RegistrationTime = DateTime.Now;
                this.LessonStudents.Remove(studentLessonModel);
                this.RegisteredStudents.Add(studentLessonModel);
            }

            _db.SaveChangesAsync();
            UpdateRegistrationInfo();
        }

        private void Register(StudentLessonEntity model) {
            model.IsRegistered = true;
            model.RegistrationTime = DateTime.Now;
            this.LessonStudents.Remove(model);
            this.RegisteredStudents.Add(model);

            _db.ThrottleSave();
        }

        private void RegisterExtStudent(StudentEntity studentEntity) {
            var studentLessonModel = new StudentLessonEntity {
                Lesson = Lesson,
                _StudentId = studentEntity.Id,
                IsRegistered = true,
                RegistrationTime = DateTime.Now
            };
            var studentLessonInfoViewModel =
                new StudentLessonInfoViewModel(studentLessonModel, AddStudentNote, ShowStudent);
            this.RegisteredStudents.Add(studentLessonInfoViewModel);
            _studentLessons.Add(studentLessonInfoViewModel);
            _db.StudentLessons.Add(studentLessonModel);
            _db.ThrottleSave();
        }

        private void UnRegister() {
            var studentModels = this.SelectedRegisteredStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonInfoViewModel studentLessonModel in studentModels) {
                if (!_internalStudentLessons.Contains(studentLessonModel)) {
                    this.RegisteredStudents.Remove(studentLessonModel);
                    _studentLessons.Remove(studentLessonModel);
                    _db.StudentLessons.Remove(studentLessonModel.StudentLesson);
                    continue;
                }

                studentLessonModel.StudentLesson.IsRegistered = false;
                studentLessonModel.StudentLesson.RegistrationTime = null;
                this.RegisteredStudents.Remove(studentLessonModel);
                this.LessonStudents.Add(studentLessonModel);
            }

            _db.SaveChanges();
            UpdateRegistrationInfo();
        }

        private async Task<BitmapImage> LoadStudentPhoto(StudentEntity entity) {
            if (entity == null) {
                return null;
            }

            try {
                var photoPath = await this.PhotoService.DownloadPhoto(StudentEntity.CardUidToId(entity.CardUid));
                return photoPath == null ? null : this.PhotoService.GetImage(photoPath);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                throw;
            }
        }

        private void UpdateDescription(StudentEntity entity) {
            if (entity == null) {
                RunInUiThread(() => { this.ActiveStudentInfoVisibility = Visibility.Collapsed; });
                return;
            }

            var now = DateTime.Now;
            var missedLessons = _db.GetStudentMissedLessons(entity, this.Lesson.Stream, now);
            var missedLectures = missedLessons.Where
            (
                lessonModel => lessonModel.Lesson.LessonType == LessonType.Lecture
            );
            var missedPractices = missedLessons
                .Where
                (
                    lessonModel => lessonModel.Lesson.LessonType == LessonType.Practice
                );
            var missedLabs = missedLessons
                .Where
                (
                    lessonModel => lessonModel.Lesson.LessonType == LessonType.Laboratory
                );
            RunInUiThread(async () => {
                this.ActiveStudentInfoVisibility = Visibility.Visible;
                this.StudentDescription = new StudentDescription {
                    Photo = await LoadStudentPhoto(entity),
                    LastName = entity.LastName,
                    FirstName = entity.FirstName,
                    SecondName = entity.SecondName,
                    GroupName = string.Join(", ", entity.Groups?.Select(group => group.Name) ?? new List<string>())
                        .Trim(),
                    LessonStat = string.Format(Localization["page.registration.active.student.info"],
                        missedLessons.Count,
                        missedLectures.Count(),
                        missedPractices.Count(),
                        missedLabs.Count()),
                };
            });
        }

        private void UpdateRegistrationInfo() {
            this.RegistrationInfoState = new LessonRegistrationInfoState {
                Registered = $"{Localization["Есть"]} {this.RegisteredStudents.Count}",
                NotRegistered = $"{Localization["Нет"]} {this.LessonStudents.Count}",
                Total = $"{Localization["Всего"]} {this.RegisteredStudents.Count + this.LessonStudents.Count}"
            };
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void StartTimer(LessonEntity entity) {
            TimeSpan timeLeft;
            var now = DateTime.Now;
            if (entity.Date.HasValue && entity.Schedule.End.HasValue) {
                timeLeft = entity.Date.Value.Date + entity.Schedule.End.Value - now;
            }
            else {
                timeLeft = TimeSpan.Zero;
            }

            this.TimerState = new TimerState {
                TimeLeft = timeLeft,
                CurrentTime = now
            };
            _timerToEnd = Observable
                .Interval(TimeSpan.FromMilliseconds(1000))
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(UpdateTimer);
        }

        private void UpdateTimer(long timeSinceTimerStart) {
            var now = DateTime.Now;

            var timeLeft = this.TimerState.TimeLeft - TimeSpan.FromMilliseconds(1000);
            if (timeLeft.TotalMilliseconds <= 0) {
                this.TimerState = new TimerState {
                    TimeLeft = TimeSpan.Zero,
                    CurrentTime = now
                };
                return;
            }

            this.TimerState = new TimerState {
                TimeLeft = timeLeft,
                CurrentTime = now
            };
        }

        private void StopTimer() {
            _timerToEnd?.Dispose();
        }

        public override void Dispose() {
            base.Dispose();
            StopTimer();
        }

        private List<StudentLessonInfoViewModel> FindInternalStudents(
            List<StudentLessonInfoViewModel> allStudentLessons,
            LessonEntity lesson) {
            var internalStudents = lesson.Group?.Students ?? lesson.Stream.Students;

            return allStudentLessons
                .Where(sl => internalStudents.Any(student => student.Id == sl.StudentLesson._StudentId))
                .ToList();
        }
    }

    public class TimerState {
        public DateTime CurrentTime { get; set; }
        public TimeSpan TimeLeft { get; set; }
    }

    public class StudentDescription {
        public BitmapImage Photo { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string SecondName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string LessonStat { get; set; } = "";
    }

    public class LessonInfoState {
        public string GroupName { get; set; }
        public string LessonInfo { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public class LessonRegistrationInfoState {
        public string Total { get; set; }
        public string Registered { get; set; }
        public string NotRegistered { get; set; }
    }

    public class StudentLessonInfoViewModel {
        public StudentLessonEntity StudentLesson { get; }
        public string GroupText { get; }
        public ICommand AddStudentNote { get; }
        public ICommand ShowStudent { get; }

        public StudentEntity Student => StudentLesson.Student;
        public DateTime? RegistrationTime => StudentLesson.RegistrationTime;

        public StudentLessonInfoViewModel(StudentLessonEntity studentLesson, ICommand addNote, ICommand showStudent) {
            StudentLesson = studentLesson;
            AddStudentNote = addNote;
            ShowStudent = showStudent;
            this.GroupText = string.Join(", ",
                studentLesson.Student.Groups?.Select(entity => entity.Name) ?? new List<string>());
        }
    }
}

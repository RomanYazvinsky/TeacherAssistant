using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
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

namespace TeacherAssistant.RegistrationPage
{
    public class RegistrationPageModel : AbstractModel<RegistrationPageModel>
    {
        private readonly TabPageHost _tabPageHost;
        private readonly WindowPageHost _windowPageHost;
        private readonly LocalDbContext _db;
        private static readonly string LocalizationKey = "page.registration";
        private StudentCardService StudentCardService { get; }
        private PhotoService PhotoService { get; }
        private List<StudentLessonEntity> _studentLessons = new List<StudentLessonEntity>();
        private List<StudentLessonEntity> _internalStudentLessons = new List<StudentLessonEntity>();
        private DispatcherTimer _timerToEnd;
        private StudentEntity _selectedStudent;


        public RegistrationPageModel(
            StudentCardService studentCardService,
            PhotoService photoService,
            TabPageHost tabPageHost,
            WindowPageHost windowPageHost,
            LocalDbContext db,
            RegistrationPageToken token,
            MainReducer mainReducer,
            PageControllerReducer reducer
        )
        {
            _tabPageHost = tabPageHost;
            _windowPageHost = windowPageHost;
            _db = db;
            this.StudentCardService = studentCardService;
            this.PhotoService = photoService;
            this.DoRegister = new CommandHandler(() =>
            {
                if (this.AllStudentsMode)
                {
                    foreach (IStudentViewModel selectedStudent in this.SelectedStudents.ToList())
                    {
                        RegisterExtStudent(selectedStudent.Student);
                    }
                }
                else
                {
                    Register();
                }
            });
            this.DoUnRegister = new CommandHandler(UnRegister);

            this.ShowStudent = new CommandHandler(() =>
            {
                var selectedStudent = _selectedStudent;
                var studentViewPageToken = new StudentViewPageToken("Student", selectedStudent);
                tabPageHost.AddPageAsync<StudentViewPageModule, StudentViewPageToken>(studentViewPageToken);
            });
            this.AddStudentNote = new CommandHandler(() =>
            {
                var selectedStudent = _selectedStudent;
                var noteFormToken = new NoteListFormToken("Note", () => new StudentNote
                {
                    Student = selectedStudent,
                    EntityId = selectedStudent.Id
                }, selectedStudent.Notes);
                windowPageHost.AddPageAsync<NoteListFormModule, NoteListFormToken>(noteFormToken);
            });
            this.ToggleAllStudentTable = new ButtonConfig
            {
                Command = new CommandHandler(() => this.AllStudentsMode = !this.AllStudentsMode),
                Text = Localization["Все студенты"]
            };
            this.AllStudentsFilter = (o, s) =>
            {
                var student = ((IStudentViewModel) o).Student;
                var alreadyAdded = _studentLessons.Any(studentLesson =>
                    studentLesson._StudentId == student.Id && studentLesson._LessonId == this.Lesson.Id);
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
            this.WhenAnyValue(model => model.TimerState).Where(LambdaHelper.NotNull).Subscribe(state =>
            {
                this.TimerString = $"{state.TimeLeft:hh\\:mm\\:ss}/{state.CurrentTime:HH:mm:ss}";
            });
            this.WhenAnyValue(model => model.Lesson).Where(LambdaHelper.NotNull).Subscribe(entity =>
            {
                var groupName = entity.Group?.Name ?? entity.Stream.Name;
                var lessonInfo =
                    $"{Localization["common.lesson.type." + entity.LessonType]}: {entity.Order}/{entity.GetLessonsCount()}";
                this.LessonInfoState = new LessonInfoState
                {
                    GroupName = groupName,
                    LessonInfo = lessonInfo,
                    Date = entity.Date?.ToString("dd.MM.yyyy"),
                    Time =
                        $"[{entity.Schedule.OrderNumber}] {entity.Schedule.Begin:hh\\:mm} - {entity.Schedule.End:hh\\:mm}"
                };
            });
            this.WhenAnyValue(model => model.AllStudentsMode).Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler).Subscribe(b =>
                {
                    this.ToggleAllStudentTable.Text = b ? Localization["Занятие"] : Localization["Все студенты"];
                    if (!b || this.AllStudents.Count != 0) return;
                    var studentViewModels = db.Students
                        .Include(model => model.Groups)
                        .ToList() // create query and load
                        .Select(model => new StudentViewModel(model))
                        .ToList();
                    this.AllStudents.AddRange(studentViewModels);
                });
            this.StudentCardService.ReadStudentCard.Subscribe(ReadStudentData);
            this.AllStudentsTableConfig = new TableConfig
            {
                Sorts = Sorts,
                Filter = this.AllStudentsFilter,
                DragConfig = new DragConfig
                {
                    SourceId = token.Id + nameof(this.AllStudentsTableConfig),
                    SourceType = nameof(StudentEntity),
                    DragValuePath = "Student"
                }
            };
            this.RegisteredStudentsTableConfig = new TableConfig
            {
                Sorts = RegisteredStudentsSorts,
                Filter = this.Filter,
                DragConfig = new DragConfig
                {
                    SourceId = token.Id + nameof(this.RegisteredStudentsTableConfig),
                    SourceType = nameof(StudentLessonEntity),
                    DropAvailability = data => data.Data.Count > 0 &&
                                               ((data.SenderType.Equals(nameof(StudentLessonEntity))
                                                 && data.SenderId.Equals(this.LessonStudentsTableConfig.DragConfig
                                                     .SourceId)
                                                )
                                                || data.SenderType.Equals(nameof(StudentEntity))),
                    Drop = async () =>
                    {
                        var dragData = await mainReducer.Select(state => state.DragData).FirstOrDefaultAsync();
                        if (dragData == null)
                        {
                            return;
                        }

                        if (nameof(StudentLessonEntity).Equals(dragData.SenderType))
                        {
                            Register();
                        }

                        if (nameof(StudentEntity).Equals(dragData.SenderType))
                        {
                            AcceptDropFromStudentTable(dragData.Data);
                        }

                        dragData.Accept();
                    },
                    DragStart = data => mainReducer.DispatchSetValueAction(state => state.DragData, data)
                }
            };
            this.LessonStudentsTableConfig = new TableConfig
            {
                Sorts = Sorts,
                Filter = this.Filter,
                DragConfig = new DragConfig
                {
                    SourceId = token.Id + nameof(this.LessonStudentsTableConfig),
                    SourceType = nameof(StudentLessonEntity),
                    DropAvailability = data => data.Data.Count > 0
                                               && data.SenderId.Equals(this.RegisteredStudentsTableConfig.DragConfig
                                                   .SourceId),
                    Drop = async () =>
                    {
                        var dragData = await mainReducer.Select(state => state.DragData).FirstAsync();
                        UnRegister();
                        dragData.Accept();
                    },
                    DragStart = data => mainReducer.DispatchSetValueAction(state => state.DragData, data)
                }
            };
            this.WhenAnyValue(model => model.IsLessonChecked).Subscribe(b =>
            {
                if (this.Lesson == null)
                {
                    return;
                }
                this.Lesson.Checked = b;
                this._db.ThrottleSave();
            });
            this.LessonStudentsTableConfig.SelectedItem
                .Where(LambdaHelper.NotNull)
                .Select(o => ((IStudentViewModel) o).Student)
                .Merge(this.RegisteredStudentsTableConfig.SelectedItem.AsObservable().Where(LambdaHelper.NotNull)
                    .Select(o => ((IStudentViewModel) o).Student))
                .Merge(this.AllStudentsTableConfig.SelectedItem.AsObservable().Where(LambdaHelper.NotNull))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe
                (
                    o =>
                    {
                        var studentEntity = o as StudentEntity;
                        _selectedStudent = studentEntity;
                        UpdateDescription(studentEntity);
                    });
            Init(token.Lesson);
            reducer.Dispatch(new RegisterControlsAction(token, GetControls()));

            this.WhenRemoved<LessonEntity>()
                .Where(entities => entities.Any(entity => entity.Id == this.Lesson?.Id))
                .Subscribe(_ =>
                {
                    token.Deactivate();
                });
        }

        private async void Init(LessonEntity lesson)
        {
            if (lesson == null)
                return;
            this.Lesson = await _db.Lessons.FindAsync(lesson.Id);
            StopTimer();
            this.LessonStudents.Clear();
            this.RegisteredStudents.Clear();
            var loadedStudentLessons = lesson.StudentLessons.ToList();
            AddMissingStudents(loadedStudentLessons, this.Lesson);
            _studentLessons = loadedStudentLessons;
            var notRegisteredStudentLessons =
                _studentLessons.Where(lessonModel => !(lessonModel.IsRegistered ?? false)).ToList();
            this.LessonStudents.AddRange(notRegisteredStudentLessons);

            var registeredStudentLesson =
                _studentLessons.Where(lessonModel => lessonModel.IsRegistered == true).ToList();
            this.RegisteredStudents.AddRange(registeredStudentLesson);
            _internalStudentLessons = FindInternalStudents(_studentLessons, lesson);
            this.IsLessonChecked = lesson.Checked;
            UpdateRegistrationInfo();
            StartTimer(this.Lesson);
        }

        private void AcceptDropFromStudentTable(List<object> list)
        {
            var studentModels = list.Cast<StudentEntity>().ToList();
            var studentIds = studentModels.Select(model => model.Id);
            var studentLessonsThatAlreadyProcessed
                = _db.StudentLessons
                    .Where(model => model._LessonId == Lesson.Id
                                    && studentIds.Any(l => model._StudentId == l))
                    .ToList();
            studentLessonsThatAlreadyProcessed.ForEach(model =>
            {
                if (model.IsLessonMissed)
                {
                    Register(model);
                }
            });
            var externalStudentsToRegister = studentModels
                .Where(model => studentLessonsThatAlreadyProcessed
                    .All(lessonModel => lessonModel._StudentId != model.Id)
                );
            foreach (var studentModel in externalStudentsToRegister)
            {
                RegisterExtStudent(studentModel);
            }

            UpdateRegistrationInfo();
        }

        public TableConfig RegisteredStudentsTableConfig { get; set; }
        public TableConfig LessonStudentsTableConfig { get; set; }


        private ObservableCollection<object> SelectedRegisteredStudents =>
            this.RegisteredStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> SelectedLessonStudents =>
            this.LessonStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> SelectedStudents => this.AllStudentsTableConfig.SelectedItems;

        private ObservableCollection<object> RegisteredStudents => this.RegisteredStudentsTableConfig.TableItems;

        private ObservableCollection<object> LessonStudents => this.LessonStudentsTableConfig.TableItems;


        private ObservableCollection<object> AllStudents => this.AllStudentsTableConfig.TableItems;

        private static Dictionary<string, ListSortDirection> RegisteredStudentsSorts { get; set; } =
            new Dictionary<string, ListSortDirection>
            {
                {nameof(StudentLessonEntity.RegistrationTime), ListSortDirection.Descending},
                {"Student.LastName", ListSortDirection.Ascending},
                {"Student.FirstName", ListSortDirection.Ascending}
            };

        private static Dictionary<string, ListSortDirection> Sorts { get; set; } =
            new Dictionary<string, ListSortDirection>
            {
                {"Student.LastName", ListSortDirection.Ascending},
                {"Student.FirstName", ListSortDirection.Ascending}
            };

        [Reactive] public LessonEntity Lesson { get; set; }

        [Reactive] public LessonInfoState LessonInfoState { get; set; }
        [Reactive] public LessonRegistrationInfoState RegistrationInfoState { get; set; }

        [Reactive] public StudentDescription StudentDescription { get; set; }

        [Reactive] public ICommand DoRegister { get; set; }

        [Reactive] public ICommand DoUnRegister { get; set; }
        [Reactive] public ICommand ShowStudent { get; set; }
        [Reactive] public ICommand AddStudentNote { get; set; }
        [Reactive] public ICommand AddStudentLessonNote { get; set; }
        [Reactive] public ButtonConfig ToggleAllStudentTable { get; set; }

        [Reactive] public bool IsAutoRegistrationEnabled { get; set; }
        [Reactive] public bool IsLessonChecked { get; set; }
        [Reactive] public bool AllStudentsMode { get; set; }
        [Reactive] public TableConfig AllStudentsTableConfig { get; set; }

        [Reactive] public Visibility ActiveStudentInfoVisibility { get; set; } = Visibility.Hidden;

        [Reactive] public TimerState TimerState { get; set; }
        [Reactive] public string TimerString { get; set; }


        private List<ButtonConfig> GetControls()
        {
            var buttonConfigs = new List<ButtonConfig>
            {
                GetRefreshButtonConfig()
            };
            buttonConfigs.Add(new ButtonConfig
            {
                Command = new CommandHandler(() =>
                {
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
            buttonConfigs.Add(new ButtonConfig
            {
                Command = ReactiveCommand.Create(() =>
                    _windowPageHost.AddPageAsync(new NoteListFormToken(
                        "Заметки",
                        () => new LessonNote
                        {
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

        public Func<object, string, bool> Filter { get; set; } = (o, s) =>
        {
            s = s.ToLowerInvariant();
            var student = ((StudentLessonEntity) o).Student;
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
        private void AddMissingStudents(List<StudentLessonEntity> studentLessonModels, LessonEntity lessonEntity)
        {
            List<StudentEntity> students;
            if (lessonEntity.Group == null)
                students = lessonEntity.Stream.Groups.Aggregate
                (
                    new List<StudentEntity>(),
                    (list, model) =>
                    {
                        list.AddRange(model.Students);
                        return list;
                    }
                );
            else
            {
                students = lessonEntity.Group.Students.ToList();
            }

            var newStudentLessonModels = students
                .Where
                (
                    studentModel => studentLessonModels.All(model => model.Student?.Id != studentModel.Id)
                )
                .Select
                (
                    model => new StudentLessonEntity
                    {
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

        private void ReadStudentData(StudentCard readData)
        {
            var student = this.LessonStudents.Cast<StudentLessonEntity>()
                .FirstOrDefault(studentModel => studentModel.Student.CardUid.Equals(readData.CardUid));
            if (student != null)
            {
                UpdateDescription(student.Student);
                if (this.IsAutoRegistrationEnabled)
                    RunInUiThread(() => Register(student));
                return;
            }

            var studentFromDatabase = _db.Students.FirstOrDefault(model => model.CardUid.Equals(readData.CardUid));
            if (studentFromDatabase != null)
            {
                var studentView = new StudentEntity();
                UpdateDescription(studentView);
                if (this.IsAutoRegistrationEnabled)
                    RunInUiThread(() =>
                    {
                        RegisterExtStudent(studentView);
                        UpdateRegistrationInfo();
                    });

                return;
            }

            var unknownStudent =
                new StudentEntity
                {
                    CardUid = readData.CardUid,
                    FirstName = readData.FirstName,
                    LastName = readData.LastName,
                    SecondName = readData.SecondName
                };
            if (readData.FullName != null)
                UpdateDescription(unknownStudent);
        }

        private void Register()
        {
            var studentLessonModels = this.SelectedLessonStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonEntity studentLessonModel in studentLessonModels)
            {
                studentLessonModel.IsRegistered = true;
                studentLessonModel.RegistrationTime = DateTime.Now;
                this.LessonStudents.Remove(studentLessonModel);
                this.RegisteredStudents.Add(studentLessonModel);
            }

            _db.SaveChangesAsync();
            UpdateRegistrationInfo();
        }

        private void Register(StudentLessonEntity model)
        {
            model.IsRegistered = true;
            model.RegistrationTime = DateTime.Now;
            this.LessonStudents.Remove(model);
            this.RegisteredStudents.Add(model);

            _db.ThrottleSave();
        }

        private void RegisterExtStudent(StudentEntity studentEntity)
        {
            var studentLessonModel = new StudentLessonEntity
            {
                Lesson = Lesson,
                Student = studentEntity,
                IsRegistered = true,
                RegistrationTime = DateTime.Now
            };
            this.RegisteredStudents.Add(studentLessonModel);
            _studentLessons.Add(studentLessonModel);
            _db.StudentLessons.Add(studentLessonModel);
            _db.ThrottleSave();
        }

        private void UnRegister()
        {
            var studentModels = this.SelectedRegisteredStudents.ToList();
            this.SelectedLessonStudents.Clear();
            this.SelectedRegisteredStudents.Clear();
            foreach (StudentLessonEntity studentLessonModel in studentModels)
            {
                if (!_internalStudentLessons.Contains(studentLessonModel))
                {
                    this.RegisteredStudents.Remove(studentLessonModel);
                    _studentLessons.Remove(studentLessonModel);
                    _db.StudentLessons.Remove(studentLessonModel);
                    continue;
                }

                studentLessonModel.IsRegistered = false;
                studentLessonModel.RegistrationTime = null;
                this.RegisteredStudents.Remove(studentLessonModel);
                this.LessonStudents.Add(studentLessonModel);
            }

            _db.SaveChanges();
            UpdateRegistrationInfo();
        }

        private async Task<BitmapImage> LoadStudentPhoto(StudentEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            try
            {
                var photoPath = await this.PhotoService.DownloadPhoto(StudentEntity.CardUidToId(entity.CardUid));
                return photoPath == null ? null : this.PhotoService.GetImage(photoPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private void UpdateDescription(StudentEntity entity)
        {
            if (entity == null)
            {
                this.ActiveStudentInfoVisibility = Visibility.Collapsed;
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
            RunInUiThread(async () =>
            {
                this.ActiveStudentInfoVisibility = Visibility.Visible;
                this.StudentDescription = new StudentDescription
                {
                    Photo = await LoadStudentPhoto(entity),
                    LastName = entity.LastName,
                    FirstName = entity.FirstName,
                    SecondName = entity.SecondName,
                    GroupName = string.Join(", ", entity.Groups.Select(group => group.Name)).Trim(),
                    LessonStat = string.Format(Localization["page.registration.active.student.info"],
                        missedLessons.Count,
                        missedLectures.Count(),
                        missedPractices.Count(),
                        missedLabs.Count()),
                };
            });
        }

        private void UpdateRegistrationInfo()
        {
            this.RegistrationInfoState = new LessonRegistrationInfoState
            {
                Registered = $"{Localization["Есть"]} {this.RegisteredStudents.Count}",
                NotRegistered = $"{Localization["Нет"]} {this.LessonStudents.Count}",
                Total = $"{Localization["Всего"]} {this.RegisteredStudents.Count + this.LessonStudents.Count}"
            };
        }

        public void Remove(StudentLessonEntity model)
        {
            if (model == null)
                return;

            this.LessonStudents.Remove(model);
            _db.StudentLessons.Remove(model);
            _db.SaveChanges();
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        private void StartTimer(LessonEntity entity)
        {
            TimeSpan timeLeft;
            var now = DateTime.Now;
            if (entity.Date.HasValue && entity.Schedule.End.HasValue)
            {
                timeLeft = entity.Date.Value.Date + entity.Schedule.End.Value - now;
            }
            else
            {
                timeLeft = TimeSpan.Zero;
            }

            this.TimerState = new TimerState
            {
                TimeLeft = timeLeft,
                CurrentTime = now
            };
            var timerToEnd = new DispatcherTimer();
            timerToEnd.Tick += (sender, args) =>
            {
                var now1 = DateTime.Now;

                var timeLeft1 = this.TimerState.TimeLeft - TimeSpan.FromMilliseconds(1000);
                if (timeLeft1.TotalMilliseconds <= 0)
                {
                    this.TimerState = new TimerState
                    {
                        TimeLeft = TimeSpan.Zero,
                        CurrentTime = now1
                    };
                    return;
                }

                this.TimerState = new TimerState
                {
                    TimeLeft = timeLeft1,
                    CurrentTime = now1
                };
            };
            timerToEnd.Interval = TimeSpan.FromMilliseconds(1000);
            timerToEnd.Start();
            _timerToEnd = timerToEnd;
        }

        private void StopTimer()
        {
            _timerToEnd?.Stop();
        }

        public override void Dispose()
        {
            base.Dispose();
            StopTimer();
        }

        private List<StudentLessonEntity> FindInternalStudents(List<StudentLessonEntity> allStudentLessons,
            LessonEntity lesson)
        {
            var internalStudents = lesson.Group?.Students ?? lesson.Stream.Students;

            return allStudentLessons.Where(sl => internalStudents.Any(student => student.Id == sl._StudentId)).ToList();
        }
    }

    public class TimerState
    {
        public DateTime CurrentTime { get; set; }
        public TimeSpan TimeLeft { get; set; }
    }

    public class StudentDescription
    {
        public BitmapImage Photo { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string SecondName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string LessonStat { get; set; } = "";
    }

    public class LessonInfoState
    {
        public string GroupName { get; set; }
        public string LessonInfo { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public class LessonRegistrationInfoState
    {
        public string Total { get; set; }
        public string Registered { get; set; }
        public string NotRegistered { get; set; }
    }
}

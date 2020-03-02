using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Database;
using TeacherAssistant.Helpers;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.RegistrationPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages.LessonForm {
    public class LessonFormModel : AbstractModel<LessonFormModel> {
        private readonly LessonFormToken _token;
        private readonly LocalDbContext _db;
        private const string LocalizationKey = "lesson.form";
        private LessonEntity _originalEntity;

        public class LessonTypeView {
            public LessonType Type { get; }

            public LessonTypeView(LessonType type) {
                Type = type;
            }

            public override string ToString() {
                return Localization[$"common.lesson.type.{Type}"];
            }
        }

        public LessonFormModel(LessonFormToken token, LocalDbContext db) {
            _token = token;
            _db = db;

            this.SaveHandler = ReactiveCommand.Create
            (
                async () => {
                    await Save();
                    token.Deactivate();
                }
            );


            this.SaveAndRegisterHandler = ReactiveCommand.Create(SaveAndOpenRegistration);
            this.CreateNewOneHandler = ReactiveCommand.Create
            (
                async () => {
                    await Save();
                    Init(new LessonEntity());
                }
            );
            this.WhenActivated(c => {
                this.WhenAnyValue(model => model.SelectedStream)
                    .Where(LambdaHelper.NotNull)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe
                    (
                        stream => {
                            this.Groups.Clear();
                            this.Groups.AddRange(stream.Groups.ToList());
                            this.SelectedGroup = this.IsGroupsAvailable ? this.Groups.FirstOrDefault() : null;
                        }
                    ).DisposeWith(c);
                this.WhenAnyValue(model => model.Lesson)
                    .Where(LambdaHelper.NotNull)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe
                    (
                        lesson => {
                            this.SelectedStream = lesson.Stream ?? this.Streams.FirstOrDefault();
                            this.Description = lesson.Description ?? "";
                            this.LessonDate = lesson.Date ?? DateTime.Today;
                            var lessonType = this.LessonTypes.Find(view => view.Type == lesson.LessonType);
                            this.SelectedLessonType = lessonType ?? this.LessonTypes.FirstOrDefault();
                            this.SelectedSchedule = lesson.Schedule ?? this.ScheduleList.FirstOrDefault();
                        }
                    ).DisposeWith(c);
                this.WhenAnyValue(model => model.SelectedLessonType)
                    .Where(LambdaHelper.NotNull)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe
                        (type => this.IsGroupsAvailable = type.Type != LessonType.Lecture)
                    .DisposeWith(c);
                this.WhenAnyValue(model => model.IsGroupsAvailable)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe
                        (isAvailable => this.SelectedGroup = isAvailable ? this.Groups.FirstOrDefault() : null)
                    .DisposeWith(c);

                WhenAdded<StreamEntity>().Merge(WhenRemoved<StreamEntity>())
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(_ => {
                        this.Streams.Clear();

                        var streamsWithGroups = _db.Streams.Include(stream => stream.Groups)
                            .Where(entity => entity._Active > 0).ToList();
                        this.Streams.AddRange(streamsWithGroups);
                    }).DisposeWith(c);
            });
            this.LessonTypes = Enum.GetValues(typeof(LessonType))
                .Cast<LessonType>()
                .Where(type => type != LessonType.Unknown)
                .Select(type => new LessonTypeView(type))
                .ToList();

            Init(token.Lesson);
        }

        private void Init(LessonEntity lesson) {
            _originalEntity = lesson.Id == default ? lesson : _db.Lessons.Find(lesson.Id);
            this.Streams.Clear();
            var streamsWithGroups = _db.Streams.Include(stream => stream.Groups)
                .Where(entity => entity._Active > 0).ToList();
            this.Streams.AddRange(streamsWithGroups);

            this.ScheduleList.Clear();
            this.ScheduleList.AddRange(_db.Schedules.ToList());
            this.Lesson = new LessonEntity(lesson);
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        [Reactive] public LessonEntity Lesson { get; set; }

        [Reactive] [NotNull] public StreamEntity SelectedStream { get; set; }
        [Reactive] [CanBeNull] public GroupEntity SelectedGroup { get; set; }

        public ObservableCollection<StreamEntity> Streams { get; set; } =
            new ObservableCollection<StreamEntity>();

        public ObservableCollection<GroupEntity> Groups { get; set; } =
            new ObservableCollection<GroupEntity>();

        [Reactive] public List<LessonTypeView> LessonTypes { get; set; }

        public ObservableCollection<ScheduleEntity> ScheduleList { get; set; } =
            new ObservableCollection<ScheduleEntity>();

        [Reactive] [NotNull] public LessonTypeView SelectedLessonType { get; set; }
        [Reactive] [NotNull] public ScheduleEntity SelectedSchedule { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public bool SetAllPresent { get; set; }

        [Reactive] public bool IsGroupsAvailable { get; set; }
        [Reactive] public DateTime LessonDate { get; set; }

        public ICommand SaveHandler { get; set; }
        public ICommand SaveAndRegisterHandler { get; set; }
        public ICommand CreateNewOneHandler { get; set; }

        private async Task Save() {
            this.Lesson.Date = this.LessonDate;
            this.Lesson.Description = this.Description;
            this.Lesson.Group = this.IsGroupsAvailable ? this.SelectedGroup : null;
            this.Lesson.Stream = this.SelectedStream;
            this.Lesson.LessonType = this.SelectedLessonType.Type;
            this.Lesson.Schedule = this.SelectedSchedule;
            if (!this.IsGroupsAvailable) {
                this.Lesson.Group = null;
            }
            _originalEntity.Apply(this.Lesson);
            _db.SetLessonOrder(_originalEntity);
            if (this.Lesson.Id == default) {
                _originalEntity.CreationDate = DateTime.Now;
                _db.Lessons.Add(_originalEntity);
            }

            await _db.SaveChangesAsync();

            if (!this.SetAllPresent) {
                return;
            }

            await RegisterAllStudents(_originalEntity);
        }

        private async Task RegisterAllStudents(LessonEntity persistentLesson) {
            List<StudentEntity> studentsShouldBeRegistered = null;
            if (this.IsGroupsAvailable) {
                if (this.SelectedGroup == null) {
                    return;
                }

                var groupStudents = this.SelectedGroup.Students?.ToList() ?? new List<StudentEntity>();
                studentsShouldBeRegistered = groupStudents;
            }
            else {
                var streamStudents = this.Groups
                    .SelectMany(group => group.Students)
                    .Distinct(new StudentEqualityComparer())
                    .ToList();
                studentsShouldBeRegistered = streamStudents;
            }

            var studentLessons = new List<StudentLessonEntity>();
            studentLessons.AddRange(_db.StudentLessons.Where(entity => entity._LessonId == this.Lesson.Id));
            var notRegisteredStudents = studentsShouldBeRegistered.Where(student =>
                studentLessons.All(studentLesson => studentLesson._StudentId != student.Id));
            var registrationTime = DateTime.Now;
            var studentLessonsToRegister = notRegisteredStudents
                .Select(student => new StudentLessonEntity {
                    Lesson = persistentLesson,
                    _LessonId = persistentLesson.Id,
                    Student = student,
                    _StudentId = student.Id,
                    IsRegistered = true,
                    RegistrationTime = registrationTime
                });
            foreach (var studentLesson in persistentLesson.StudentLessons ?? new List<StudentLessonEntity>()) {
                studentLesson.IsRegistered = true;
                studentLesson.RegistrationTime = studentLesson.RegistrationTime ?? registrationTime;
            }

            _db.StudentLessons.AddRange(studentLessonsToRegister);
            await _db.SaveChangesAsync();
        }

        private async Task SaveAndOpenRegistration() {
            await Save();
            await _token.PageHost.AddPageAsync(new RegistrationPageToken("Регистрация", _originalEntity));
            _token.Deactivate();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.CommonStudentLessonViewPage;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.Pages.RegistrationPage;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.SchedulePage
{
    public class LessonScheduleView
    {
        public LessonEntity Lesson { get; }

        public string Date { get; }

        public string GroupNames => this.Lesson._GroupId == null
            ? string.Join(", ", this.Lesson.Stream.Groups?.Select(model => model.Name) ?? new string[]{})
            : this.Lesson.Group.Name;

        public string LocalizedType { get; }

        public Brush IconColor { get; }

        public SchedulePageModel Model { get; }

        public LessonScheduleView(SchedulePageModel sm, LessonEntity entity)
        {
            this.Lesson = entity;
            this.Model = sm;
            this.LocalizedType = LocalizationContainer.Localization[$"common.lesson.type.{entity.LessonType}"];
            this.Date = $"{entity.Schedule?.Begin:hh\\:mm}-{entity.Schedule?.End:hh\\:mm}";
            this.IconColor = this.Lesson.Checked ? Brushes.Black : Brushes.Red;
        }
    }


    public class ScheduleComboboxItem : ScheduleEntity
    {
        public override string ToString()
        {
            return LocalizationContainer.Localization["common.empty.dropdown"];
        }
    }

    public class LessonTypeComboboxItem
    {
        public LessonType Type { get; }

        public LessonTypeComboboxItem(LessonType type)
        {
            this.Type = type;
        }

        public override string ToString()
        {
            return LocalizationContainer.Localization[
                this.Type == LessonType.Unknown ? "common.empty.dropdown" : $"common.lesson.type.{this.Type}"];
        }
    }

    public class SchedulePageModel : AbstractModel<SchedulePageModel>
    {
        private readonly WindowComponentHost _windowComponentHost;
        private readonly IComponentHost _currentHost;
        private readonly LocalDbContext _context;
        private const string LocalizationKey = "page.schedule";

        private static readonly StreamEntity EmptyStream = new StreamEntity
        {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly GroupEntity EmptyGroup = new GroupEntity
        {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly ScheduleComboboxItem EmptyScheduleComboboxItem = new ScheduleComboboxItem {Id = -1};

        public SchedulePageModel(WindowComponentHost windowComponentHost, IComponentHost currentHost, LocalDbContext context)
        {
            _windowComponentHost = windowComponentHost;
            _currentHost = currentHost;
            _context = context;
            this.OpenRegistrationHandler = ReactiveCommand.Create(OpenRegistration);
            this.DeleteMenuHandler = ReactiveCommand.Create(DeleteSelectedLesson);
            this.EditMenuHandler = ReactiveCommand.Create(OpenLessonEditForm);
            this.OpenStudentLessonTableHandler = ReactiveCommand.Create(OpenStudentLessonTable);
            this.OpenLessonNotesHandler = ReactiveCommand.Create(OpenLessonNotes);
            this.ToggleLessonCheckedHandler = ReactiveCommand.Create(ToggleLessonChecked);
            this.Show = ReactiveCommand.Create(ShowLessons);
            Init();
            this.WhenActivated(c => {
                this.WhenAnyValue(model => model.SelectedStream)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(UpdateSelectedStream)
                    .DisposeWith(c);
                Observable.Merge(
                        this.WhenAdded<LessonEntity>(),
                        this.WhenRemoved<LessonEntity>(),
                        this.WhenUpdated<LessonEntity>()
                    ).ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(_ => ShowLessons())
                    .DisposeWith(c);
            });
        }

        private void UpdateSelectedStream(StreamEntity stream) {
            List<GroupEntity> groups;
            if (stream == null || stream.Id == -1)
            {
                groups = _context.Groups.ToList();
            }
            else
            {
                groups = stream.Groups?.ToList() ?? new List<GroupEntity>();
            }
            this.Groups.Clear();
            this.Groups.Add(EmptyGroup);
            this.Groups.AddRange(groups);
            if (this.SelectedGroup == null
                || this.Groups.FirstOrDefault(model => this.SelectedGroup.Id == model.Id) != null)
            {
                this.SelectedGroup = EmptyGroup;
            }
        }

        private async Task Init() {
            var streamModels = await _context.Streams.ToListAsync();
            var schedules = await _context.Schedules.ToListAsync();
            var groups = await _context.Groups.ToListAsync();
            RunInUiThread(() => {
                this.Streams.Clear();
                this.Groups.Clear();
                this.Schedules.Clear();
                this.Streams.Add(EmptyStream);
                this.Groups.Add(EmptyGroup);
                this.Schedules.Add(EmptyScheduleComboboxItem);
                this.Streams.AddRange(streamModels);
                this.Schedules.AddRange(schedules);
                this.Groups.AddRange(groups);
                this.SelectedGroup = EmptyGroup;
                this.SelectedLessonType = this.LessonTypes.FirstOrDefault();
                this.SelectedSchedule = EmptyScheduleComboboxItem;
                this.SelectedStream = EmptyStream;
                ShowLessons();
            });
        }


        public ObservableCollection<LessonScheduleView> Lessons { get; set; } =
            new ObservableCollection<LessonScheduleView>();

        public ObservableCollection<StreamEntity> Streams { get; set; } =
            new ObservableCollection<StreamEntity>();

        public ObservableCollection<GroupEntity> Groups { get; set; } =
            new ObservableCollection<GroupEntity>();

        public ObservableCollection<ScheduleEntity> Schedules { get; set; } =
            new ObservableCollection<ScheduleEntity>();

        [Reactive] [CanBeNull] public LessonScheduleView SelectedLesson { get; set; }

        [Reactive] public ICommand Show { get; set; }


        [Reactive] public GroupEntity SelectedGroup { get; set; }

        [Reactive] public StreamEntity SelectedStream { get; set; }

        [Reactive] public ScheduleEntity SelectedSchedule { get; set; }


        public List<LessonTypeComboboxItem> LessonTypes { get; set; } =
            new List<LessonTypeComboboxItem>
            (
                Enum.GetValues(typeof(LessonType))
                    .Cast<LessonType>()
                    .Select(type => new LessonTypeComboboxItem(type))
                    .ToList()
            );

        [Reactive] public LessonTypeComboboxItem SelectedLessonType { get; set; }

        [Reactive] public DateTime DateFrom { get; set; } = DateTime.Today;

        [Reactive] public DateTime DateTo { get; set; } = DateTime.Today;
        public ICommand DeleteMenuHandler { get; set; }
        public ICommand EditMenuHandler { get; set; }
        public ICommand OpenRegistrationHandler { get; }
        public ICommand OpenStudentLessonTableHandler { get; }
        public ICommand OpenLessonNotesHandler { get; }
        public ICommand ToggleLessonCheckedHandler { get; }


        private void ShowLessons()
        {
            this.Lessons.Clear();
            var lessonScheduleViews = GetLessonsByCriteria()
                .Select(model => new LessonScheduleView(this, model))
                .ToList();
            this.Lessons.AddRange(lessonScheduleViews);
        }

        /// <summary>
        ///     SQLite EF6 provider does not support TruncateTime and other functions.
        /// </summary>
        private IEnumerable<LessonEntity> GetLessonsByCriteria()
        {
            var query = _context.Lessons
                .Where(model => model._Date != null)
                .Include(lesson => lesson.Schedule)
                .Include(lesson => lesson.Group.Streams)
                .Include(lesson => lesson.Notes)
                .Include(lesson => lesson.Stream.Groups);
            if (this.SelectedStream != null && this.SelectedStream.Id != -1)
                query = query.Where(model => model.Stream.Id == this.SelectedStream.Id);

            if (this.SelectedSchedule != null && this.SelectedSchedule.Id != -1)
                query = query.Where(model => model.Schedule.Id == this.SelectedSchedule.Id);

            if (this.SelectedLessonType != null && this.SelectedLessonType.Type != LessonType.Unknown)
            {
                query = query.Where(model => model._TypeId == (long) this.SelectedLessonType.Type);
            }

            if (this.SelectedGroup != null && this.SelectedGroup.Id != -1)
                query = query.Where
                (
                    model =>
                        model.Group != null
                            ? model.Group.Id == this.SelectedGroup.Id
                            : model.Stream.Groups.Any
                            (
                                groupModel =>
                                    groupModel.Id == this.SelectedGroup.Id
                            )
                );

            return query.AsEnumerable().Where(model => model.Date >= this.DateFrom && model.Date <= this.DateTo);
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        private void OpenLessonEditForm()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            var lesson = this.SelectedLesson.Lesson;
            var lessonFormModuleToken = new LessonFormToken("Lesson", lesson, _currentHost);
            _windowComponentHost.AddPageAsync(lessonFormModuleToken);
        }

        private void OpenRegistration()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            _currentHost.AddPageAsync(new RegistrationPageToken("Регистрация", this.SelectedLesson.Lesson));
        }

        private void OpenStudentLessonTable()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            _currentHost.AddPageAsync(new TableLessonViewToken("Занятие 1", this.SelectedLesson.Lesson));
        }

        private async void DeleteSelectedLesson()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            var messageBoxResult = MessageBox.Show(Localization["Вы уверены, что хотите удалить занятие?"],
                Localization["Подтверждение удаления"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return;
            }
            var lessonEntity = await _context.Lessons.FindAsync(this.SelectedLesson.Lesson.Id);
            if (lessonEntity == null)
            {
                ShowLessons();
                return;
            }

            _context.Lessons.Remove(lessonEntity);
            await _context.SaveChangesAsync();
        }

        private async void ToggleLessonChecked()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            var lessonEntity = await _context.Lessons.FindAsync(this.SelectedLesson.Lesson.Id);
            if (lessonEntity == null)
            {
                ShowLessons();
                return;
            }

            lessonEntity.Checked = !lessonEntity.Checked;
            await _context.SaveChangesAsync();
        }

        private void OpenLessonNotes()
        {
            if (this.SelectedLesson == null)
            {
                return;
            }

            var selectedLesson = this.SelectedLesson.Lesson;
            _windowComponentHost.AddPageAsync(new NoteListFormToken("Заметки", () => new LessonNote
            {
                Lesson = selectedLesson,
                EntityId = selectedLesson.Id
            }, selectedLesson.Notes));
        }
    }
}

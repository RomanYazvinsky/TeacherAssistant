using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Containers.Annotations;
using DynamicData;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Pages.CommonStudentLessonViewPage;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.RegistrationPage;

namespace TeacherAssistant.ComponentsImpl.SchedulePage
{
    public class LessonScheduleView
    {
        public LessonEntity Lesson { get; }

        public string Date { get; }

        public string GroupNames => this.Lesson._GroupId == null
            ? string.Join(", ", this.Lesson.Stream.Groups.Select(model => model.Name))
            : this.Lesson.Group.Name;

        public string LocalizedType { get; }

        public Brush IconColor { get; }

        public bool TooltipVisibility { get; }

        public SchedulePageModel Model { get; }

        public LessonScheduleView(SchedulePageModel sm, LessonEntity entity)
        {
            this.Lesson = entity;
            this.Model = sm;
            this.LocalizedType = LocalizationContainer.Localization[$"common.lesson.type.{entity.LessonType}"];
            this.Date = $"{entity.Schedule?.Begin:hh\\:mm}-{entity.Schedule?.End:hh\\:mm}";
            this.IconColor = this.Lesson.Checked ? Brushes.Black : Brushes.Red;
            this.TooltipVisibility = entity.Notes?.Count > 0;
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
        private readonly WindowPageHost _windowPageHost;
        private readonly IPageHost _currentHost;
        private readonly LocalDbContext _context;
        private const string LocalizationKey = "page.schedule";

        public static readonly string SelectedLessonKey = "SelectedLesson";

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

        public SchedulePageModel(WindowPageHost windowPageHost, IPageHost currentHost, LocalDbContext context)
        {
            _windowPageHost = windowPageHost;
            _currentHost = currentHost;
            _context = context;
            this.OpenRegistrationHandler = ReactiveCommand.Create(OpenRegistration);
            this.DeleteMenuHandler = ReactiveCommand.Create(DeleteSelectedLesson);
            this.EditMenuHandler = ReactiveCommand.Create(OpenLessonEditForm);
            this.OpenStudentLessonTableHandler = ReactiveCommand.Create(OpenStudentLessonTable);
            this.OpenLessonNotesHandler = ReactiveCommand.Create(OpenLessonNotes);
            this.ToggleLessonCheckedHandler = ReactiveCommand.Create(ToggleLessonChecked);
            this.Show = ReactiveCommand.Create(ShowLessons);
            this.WhenAnyValue(model => model.SelectedStream)
                .Subscribe
                (
                    stream =>
                    {
                        List<GroupEntity> groups;
                        if (stream == null || stream.Id == -1)
                        {
                            groups = context.Groups.ToList();
                        }
                        else
                        {
                            groups = stream.Groups.ToList();
                        }

                        groups.Insert(0, EmptyGroup);
                        this.Groups.Clear();
                        this.Groups.AddRange(groups);
                        if (this.SelectedGroup == null
                            || this.Groups.FirstOrDefault(model => this.SelectedGroup.Id == model.Id) != null)
                        {
                            this.SelectedGroup = EmptyGroup;
                        }
                    }
                );
            this.RefreshSubject.AsObservable()
                .Subscribe
                (
                    async _ =>
                    {
                        this.Streams.Clear();
                        this.Groups.Clear();
                        var streamModels = await context.Streams.ToListAsync();
                        streamModels.Insert(0, EmptyStream); // default value
                        this.Streams.AddRange(streamModels);
                        var schedules = await context.Schedules.ToListAsync();
                        schedules.Insert(0, EmptyScheduleComboboxItem);
                        schedules.ForEach(this.Schedules.Add);
                        (await context.Groups.ToListAsync()).ForEach(this.Groups.Add);
                        this.Groups.Insert(0, EmptyGroup);
                        this.SelectedGroup = EmptyGroup;
                        this.SelectedLessonType = this.LessonTypes[0];
                        this.SelectedSchedule = EmptyScheduleComboboxItem;
                        this.SelectedStream = EmptyStream;
                        ShowLessons();
                    }
                );
            Observable.Merge(
                    this.WhenAdded<LessonEntity>(),
                    this.WhenRemoved<LessonEntity>(),
                    this.WhenUpdated<LessonEntity>()
                ).ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => ShowLessons());
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
            this.Lessons.AddRange(
                BuildQuery()
                    .Select(model => new LessonScheduleView(this, model))
                    .ToList()
            );
        }

        /// <summary>
        ///     SQLite EF6 provider does not support TruncateTime and other functions.
        /// </summary>
        private IEnumerable<LessonEntity> BuildQuery()
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
            _windowPageHost.AddPageAsync<LessonFormModule, LessonFormToken>(lessonFormModuleToken);
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
            _windowPageHost.AddPageAsync(new NoteListFormToken("Заметки", () => new LessonNote
            {
                Lesson = selectedLesson,
                EntityId = selectedLesson.Id
            }, selectedLesson.Notes));
        }
    }
}

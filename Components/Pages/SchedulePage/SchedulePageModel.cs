using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Dao;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.RegistrationPage;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl.SchedulePage {
    public class LessonScheduleView {
        public LessonEntity Lesson { get; }

        public string GroupNames => this.Lesson._GroupId == null
            ? string.Join(", ", this.Lesson.Stream.Groups.Select(model => model.Name))
            : this.Lesson.Group.Name;

        public string LocalizedType { get; }

        public SchedulePageModel Model { get; }

        public LessonScheduleView(SchedulePageModel sm, LessonEntity entity) {
            this.Lesson = entity;
            this.Model = sm;
            this.LocalizedType = AbstractModel.Localization[$"common.lesson.type.{entity.LessonType}"];
        }
    }


    public class ScheduleComboboxItem : ScheduleEntity {
        public override string ToString() {
            return AbstractModel.Localization["common.empty.dropdown"];
        }
    }

    public class LessonTypeComboboxItem {
        public LessonType Type { get; }

        public LessonTypeComboboxItem(LessonType type) {
            this.Type = type;
        }

        public override string ToString() {
            return AbstractModel.Localization[
                this.Type == LessonType.Unknown ? "common.empty.dropdown" : $"common.lesson.type.{this.Type}"];
        }
    }

    public class SchedulePageModel : AbstractModel {
        private readonly TabPageHost _host;
        private const string LocalizationKey = "page.schedule";

        public static readonly string SelectedLessonKey = "SelectedLesson";

        private static readonly StreamEntity EmptyStream = new StreamEntity {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly GroupEntity EmptyGroup = new GroupEntity {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly ScheduleComboboxItem EmptyScheduleComboboxItem = new ScheduleComboboxItem {Id = -1};

        public SchedulePageModel(TabPageHost host, LocalDbContext context) {
            _host = host;
            this.DeleteMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler
                (
                    () => { }
                )
            };

            this.EditMenuButtonConfig = new ButtonConfig {
                Command = new CommandHandler
                (
                    () => {
                        var lesson = this.SelectedLesson.Lesson;
                        var lessonFormModuleToken = new LessonFormToken("Lesson", lesson, _host);
                        host.AddPageAsync<LessonFormModule, LessonFormToken>(lessonFormModuleToken);
                    }
                )
            };

            this.Show = new CommandHandler
            (
                () => {
                    this.Lessons.Clear();
                    this.Lessons.AddRange(BuildQuery().Select(model => new LessonScheduleView(this, model)).ToList());
                }
            );
            this.WhenAnyValue(model => model.SelectedStream)
                .Subscribe
                (
                    stream => {
                        List<GroupEntity> groups;
                        if (stream == null || stream.Id == -1) {
                            groups = context.Groups.ToList();
                        }
                        else {
                            groups = stream.Groups.ToList();
                        }

                        groups.Insert(0, EmptyGroup);
                        this.Groups.Clear();
                        this.Groups.AddRange(groups);
                        if (this.SelectedGroup == null
                            || this.Groups.FirstOrDefault(model => this.SelectedGroup.Id == model.Id) != null) {
                            this.SelectedGroup = EmptyGroup;
                        }
                    }
                );
            this.RefreshSubject.AsObservable()
                .Subscribe
                (
                    async _ => {
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
                    }
                );
        }


        public ObservableRangeCollection<LessonScheduleView> Lessons { get; set; } =
            new WpfObservableRangeCollection<LessonScheduleView>();

        public ObservableRangeCollection<StreamEntity> Streams { get; set; } =
            new WpfObservableRangeCollection<StreamEntity>();

        public ObservableRangeCollection<GroupEntity> Groups { get; set; } =
            new WpfObservableRangeCollection<GroupEntity>();

        public ObservableRangeCollection<ScheduleEntity> Schedules { get; set; } =
            new WpfObservableRangeCollection<ScheduleEntity>();

        [Reactive] public LessonScheduleView SelectedLesson { get; set; }

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
        [Reactive] public ButtonConfig DeleteMenuButtonConfig { get; set; }
        [Reactive] public ButtonConfig EditMenuButtonConfig { get; set; }

        /// <summary>
        ///     SQLite EF6 provider does not support TruncateTime and other functions.
        /// </summary>
        private IEnumerable<LessonEntity> BuildQuery() {
            var query = LocalDbContext.Instance.Lessons
                .Where(model => model._Date != null)
                .Include(lesson => lesson.Schedule)
                .Include(lesson => lesson.Group.Streams)
                .Include(lesson => lesson.Stream.Groups);
            if (this.SelectedStream != null && this.SelectedStream.Id != -1)
                query = query.Where(model => model.Stream.Id == this.SelectedStream.Id);

            if (this.SelectedSchedule != null && this.SelectedSchedule.Id != -1)
                query = query.Where(model => model.Schedule.Id == this.SelectedSchedule.Id);

            if (this.SelectedLessonType != null && this.SelectedLessonType.Type != LessonType.Unknown) {
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

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public void OpenRegistration() {
            _host.AddPageAsync(new RegistrationPageToken("Регистрация", this.SelectedLesson.Lesson));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl.SchedulePage {
    public class LessonScheduleView {
        public LessonModel Lesson { get; }

        public string GroupNames => this.Lesson._GroupId == null
                                        ? string.Join(", ", this.Lesson._Stream.Groups.Select(model => model.Name))
                                        : this.Lesson._Group.Name;

        public string LocalizedType { get; }

        public SchedulePageModel Model { get; }

        public LessonScheduleView(SchedulePageModel sm, LessonModel model) {
            this.Lesson = model;
            this.Model = sm;
            this.LocalizedType = AbstractModel.Localization[$"common.lesson.type.{model.LessonType}"];
        }
    }


    public class ScheduleComboboxItem : ScheduleModel {
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
        private const string LocalizationKey = "page.schedule";

        public static readonly string SelectedLessonKey = "SelectedLesson";

        private static readonly StreamModel EmptyStream = new StreamModel {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly GroupModel EmptyGroup = new GroupModel {
            Id = -1,
            Name = Localization["common.empty.dropdown"]
        };

        private static readonly ScheduleComboboxItem EmptyScheduleComboboxItem = new ScheduleComboboxItem {Id = -1};

        public SchedulePageModel(string id) : base(id) {
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
                        var newId = this.PageService.OpenPage
                            ("Modal",  new PageProperties{PageType = typeof(LessonForm)});
                        StoreManager.Publish(this.SelectedLesson.Lesson, newId, "LessonChange");
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
                         List<GroupModel> groups;
                         if (stream == null || stream.Id == -1) {
                             groups = _db.GroupModels.ToList();
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
        }


        public ObservableRangeCollection<LessonScheduleView> Lessons { get; set; } =
            new WpfObservableRangeCollection<LessonScheduleView>();

        public ObservableRangeCollection<StreamModel> Streams { get; set; } =
            new WpfObservableRangeCollection<StreamModel>();

        public ObservableRangeCollection<GroupModel> Groups { get; set; } =
            new WpfObservableRangeCollection<GroupModel>();

        public ObservableRangeCollection<ScheduleModel> Schedules { get; set; } =
            new WpfObservableRangeCollection<ScheduleModel>();

        [Reactive] public LessonScheduleView SelectedLesson { get; set; }

        [Reactive] public ICommand Show { get; set; }


        [Reactive] public GroupModel SelectedGroup { get; set; }

        [Reactive] public StreamModel SelectedStream { get; set; }

        [Reactive] public ScheduleModel SelectedSchedule { get; set; }

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
        private IEnumerable<LessonModel> BuildQuery() {
            var query = _db.LessonModels.Where(model => model._Date != null)
                           .Include("Schedule")
                           .Include("_Group.Streams")
                           .Include("_Stream.Groups"); // eager loading. Using name instead of lambda is faster
            if (this.SelectedStream != null && this.SelectedStream.Id != -1)
                query = query.Where(model => model._Stream.Id == this.SelectedStream.Id);

            if (this.SelectedSchedule != null && this.SelectedSchedule.Id != -1)
                query = query.Where(model => model.Schedule.Id == this.SelectedSchedule.Id);

            if (this.SelectedLessonType != null && this.SelectedLessonType.Type != LessonType.Unknown) {
                query = query.Where(model => model._TypeId == (long) this.SelectedLessonType.Type);
            }

            if (this.SelectedGroup != null && this.SelectedGroup.Id != -1)
                query = query.Where
                (
                    model =>
                        model._Group != null
                            ? model._Group.Id == this.SelectedGroup.Id
                            : model._Stream.Groups.Any
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
            var lessonTabId = this.PageService.OpenPage
                (PageConfigs.RegistrationPageConfig, this.Id);
            StoreManager.Publish(this.SelectedLesson.Lesson, lessonTabId, SelectedLessonKey);
        }

        public override Task Init() {
            Select<object>("")
               .Subscribe
                (
                    async _ => {
                        this.Streams.Clear();
                        this.Groups.Clear();
                        var streamModels = await _db.StreamModels.ToListAsync();
                        streamModels.Insert(0, EmptyStream); // default value
                        this.Streams.AddRange(streamModels);
                        var schedules = await _db.ScheduleModels.ToListAsync();
                        schedules.Insert(0, EmptyScheduleComboboxItem);
                        schedules.ForEach(this.Schedules.Add);
                        (await _db.GroupModels.ToListAsync()).ForEach(this.Groups.Add);
                        this.Groups.Insert(0, EmptyGroup);
                        this.SelectedGroup = EmptyGroup;
                        this.SelectedLessonType = this.LessonTypes[0];
                        this.SelectedSchedule = EmptyScheduleComboboxItem;
                        this.SelectedStream = EmptyStream;
                    }
                );
            return Task.CompletedTask;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Pages.LessonForm {
    public class LessonFormModel : AbstractModel {
        private const string LocalizationKey = "lesson.form";
        private LessonModel _originalModel;

        public class LessonTypeView {
            public LessonType Type;

            public LessonTypeView(LessonType type) {
                Type = type;
            }

            public override string ToString() {
                return Localization[$"common.lesson.type.{Type}"];
            }
        }

        public LessonFormModel(string id) : base(id) {
            WhenAdded<StreamModel>().Subscribe(models => UpdateFromAsync(() => this.Streams.AddRange(models.ToList())));
            WhenRemoved<StreamModel>()
               .Subscribe(models => UpdateFromAsync(() => this.Streams.RemoveRange(models.ToList())));
            this.Streams.AddRange(_db.StreamModels.ToList());
            this.ScheduleList.AddRange(_db.ScheduleModels.ToList());
            this.SaveButtonConfig = new ButtonConfig {
                Text = Localization["Save"],
                Command = new CommandHandler
                (
                    () => {
                        Save();
                        this.PageService.ClosePage(id);
                    }
                )
            };


            this.RegisterButtonConfig = new ButtonConfig {
                Text = Localization["Register"],
                Command = new CommandHandler(SaveAndOpenRegistration)
            };
            this.NewOneButtonConfig = new ButtonConfig {
                Text = Localization["New one"],
                Command = new CommandHandler
                (
                    () => {
                        Save();
                        StoreManager.Publish(this.Id + ".LessonChange", new LessonModel());
                    }
                )
            };
            this.LessonTypes = (Enum.GetValues(typeof(LessonType)))
                              .Cast<LessonType>()
                              .Where(type => type != LessonType.Unknown)
                              .Select(type => new LessonTypeView(type))
                              .ToList();
            this.WhenAnyValue(model => model.SelectedStream)
                .Where(NotNull)
                .Subscribe
                 (
                     stream => {
                         this.Groups.Clear();
                         this.Groups.AddRange(stream.Groups.ToList());
                         this.Lesson.Group = this.Groups.FirstOrDefault();
                     }
                 );
            this.WhenAnyValue(model => model.Lesson)
                .Where(NotNull)
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
                 );
            this.WhenAnyValue(model => model.SelectedLessonType)
                .Where(NotNull)
                .Subscribe
                 (
                     type => {
                         this.Lesson.LessonType = type.Type;
                         this.IsGroupsAvailable = this.Lesson.LessonType != LessonType.Lecture;
                     }
                 );
            this.WhenAnyValue(model => model.SelectedSchedule)
                .Where(NotNull)
                .Subscribe
                 (
                     schedule => { this.Lesson.Schedule = schedule; }
                 );
            this.WhenAnyValue(model => model.IsGroupsAvailable)
                .Where(b => this.Lesson != null)
                .Subscribe
                 (
                     isAvailable => { this.Lesson.Group = isAvailable ? this.Groups.FirstOrDefault() : null; }
                 );
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        [Reactive] public LessonModel Lesson { get; set; }

        [Reactive] public StreamModel SelectedStream { get; set; }

        public ObservableRangeCollection<StreamModel> Streams { get; set; } =
            new WpfObservableRangeCollection<StreamModel>();

        public ObservableRangeCollection<GroupModel> Groups { get; set; } =
            new WpfObservableRangeCollection<GroupModel>();

        [Reactive] public List<LessonTypeView> LessonTypes { get; set; }

        public ObservableRangeCollection<ScheduleModel> ScheduleList { get; set; } =
            new WpfObservableRangeCollection<ScheduleModel>();

        [Reactive] public LessonTypeView SelectedLessonType { get; set; }
        [Reactive] public ScheduleModel SelectedSchedule { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public bool SetAllPresent { get; set; }

        [Reactive] public bool IsGroupsAvailable { get; set; }
        [Reactive] public DateTime LessonDate { get; set; }

        public ButtonConfig SaveButtonConfig { get; set; }
        public ButtonConfig RegisterButtonConfig { get; set; }
        public ButtonConfig NewOneButtonConfig { get; set; }

        private void Save() {
            this.Lesson.Date = this.LessonDate;
            this.Lesson.Description = this.Description;
            this.Lesson.Stream = this.SelectedStream;
            this.Lesson.LessonType = this.SelectedLessonType.Type;
            if (!this.IsGroupsAvailable) {
                this.Lesson.Group = null;
            }

            _originalModel.Apply(this.Lesson);
            if (this.Lesson.Id == 0) {
                _originalModel.CreationDate = DateTime.Now;
                _db.LessonModels.Add(_originalModel);
            }

            _db.SaveChangesAsync();
        }

        private void SaveAndOpenRegistration() {
            Save();
            this.PageService.ClosePage(this.Id);
            var newId = this.PageService.OpenPage(PageConfigs.SchedulePageConfig, this.Id);
            StoreManager.Publish
            (
                this.Lesson,
                newId,
                "SelectedLesson"
            );
        }

        public override Task Init() {
            Select<LessonModel>(this.Id + ".LessonChange")
               .Where(NotNull)
               .Subscribe
                (
                    lesson => {
                        _originalModel = lesson;
                        this.Lesson = new LessonModel(lesson);
                    }
                );
            return Task.CompletedTask;
        }
    }
}
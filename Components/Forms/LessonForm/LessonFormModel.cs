﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Containers;
using DynamicData;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Dao;
using TeacherAssistant.RegistrationPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages.LessonForm
{
    public class LessonFormModel : AbstractModel<LessonFormModel>
    {
        private readonly LessonFormToken _token;
        private readonly LocalDbContext _db;
        private const string LocalizationKey = "lesson.form";
        private LessonEntity _originalEntity;

        public class LessonTypeView
        {
            public LessonType Type;

            public LessonTypeView(LessonType type)
            {
                Type = type;
            }

            public override string ToString()
            {
                return Localization[$"common.lesson.type.{Type}"];
            }
        }

        public LessonFormModel(LessonFormToken token, LocalDbContext db)
        {
            _token = token;
            _db = db;
            this.Streams.AddRange(_db.Streams.Where(entity => entity._Active > 0).ToList());
            this.ScheduleList.AddRange(_db.Schedules.ToList());
            this.SaveButtonConfig = new ButtonConfig
            {
                Text = Localization["Save"],
                Command = new CommandHandler
                (
                    () =>
                    {
                        Save();
                        token.Deactivate();
                    }
                )
            };


            this.RegisterButtonConfig = new ButtonConfig
            {
                Text = Localization["Register"],
                Command = new CommandHandler(SaveAndOpenRegistration)
            };
            this.NewOneButtonConfig = new ButtonConfig
            {
                Text = Localization["New one"],
                Command = new CommandHandler
                (
                    () =>
                    {
                        Save();
                        Init(new LessonEntity());
                    }
                )
            };
            this.LessonTypes = Enum.GetValues(typeof(LessonType))
                .Cast<LessonType>()
                .Where(type => type != LessonType.Unknown)
                .Select(type => new LessonTypeView(type))
                .ToList();
            this.WhenAnyValue(model => model.SelectedStream)
                .Where(LambdaHelper.NotNull)
                .Subscribe
                (
                    stream =>
                    {
                        this.Groups.Clear();
                        this.Groups.AddRange(stream.Groups.ToList());
                        this.Lesson.Group = this.Groups.FirstOrDefault();
                    }
                );
            this.WhenAnyValue(model => model.Lesson)
                .Where(LambdaHelper.NotNull)
                .Subscribe
                (
                    lesson =>
                    {
                        this.SelectedStream = lesson.Stream ?? this.Streams.FirstOrDefault();
                        this.Description = lesson.Description ?? "";
                        this.LessonDate = lesson.Date ?? DateTime.Today;
                        var lessonType = this.LessonTypes.Find(view => view.Type == lesson.LessonType);
                        this.SelectedLessonType = lessonType ?? this.LessonTypes.FirstOrDefault();
                        this.SelectedSchedule = lesson.Schedule ?? this.ScheduleList.FirstOrDefault();
                    }
                );
            this.WhenAnyValue(model => model.SelectedLessonType)
                .Where(LambdaHelper.NotNull)
                .Subscribe
                (
                    type =>
                    {
                        this.Lesson.LessonType = type.Type;
                        this.IsGroupsAvailable = this.Lesson.LessonType != LessonType.Lecture;
                    }
                );
            this.WhenAnyValue(model => model.SelectedSchedule)
                .Where(LambdaHelper.NotNull)
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

            WhenAdded<StreamEntity>().Merge(WhenRemoved<StreamEntity>())
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(models =>
                {
                    this.Streams.Clear();
                    this.Streams.AddRange(_db.Streams.Where(entity => entity._Active > 0).ToList());
                });
            Init(token.Lesson);
        }

        private void Init(LessonEntity lesson)
        {
            _originalEntity = lesson;
            this.Lesson = new LessonEntity(lesson);
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        [Reactive] public LessonEntity Lesson { get; set; }

        [Reactive] public StreamEntity SelectedStream { get; set; }

        public ObservableCollection<StreamEntity> Streams { get; set; } =
            new ObservableCollection<StreamEntity>();

        public ObservableCollection<GroupEntity> Groups { get; set; } =
            new ObservableCollection<GroupEntity>();

        [Reactive] public List<LessonTypeView> LessonTypes { get; set; }

        public ObservableCollection<ScheduleEntity> ScheduleList { get; set; } =
            new ObservableCollection<ScheduleEntity>();

        [Reactive] public LessonTypeView SelectedLessonType { get; set; }
        [Reactive] public ScheduleEntity SelectedSchedule { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public bool SetAllPresent { get; set; }

        [Reactive] public bool IsGroupsAvailable { get; set; }
        [Reactive] public DateTime LessonDate { get; set; }

        public ButtonConfig SaveButtonConfig { get; set; }
        public ButtonConfig RegisterButtonConfig { get; set; }
        public ButtonConfig NewOneButtonConfig { get; set; }

        private async Task Save()
        {
            this.Lesson.Date = this.LessonDate;
            this.Lesson.Description = this.Description;
            this.Lesson.Stream = this.SelectedStream;
            this.Lesson.LessonType = this.SelectedLessonType.Type;
            if (!this.IsGroupsAvailable)
            {
                this.Lesson.Group = null;
            }

            _originalEntity.Apply(this.Lesson);
            if (this.Lesson.Id == 0)
            {
                _originalEntity.CreationDate = DateTime.Now;
                _db.SetLessonOrder(_originalEntity);
                _db.Lessons.Add(_originalEntity);
            }

            await _db.SaveChangesAsync();
        }

        private async Task SaveAndOpenRegistration()
        {
            await Save();
            await _token.PageHost.AddPageAsync(new RegistrationPageToken("Registration", this.Lesson));
            _token.Deactivate();
        }
    }
}

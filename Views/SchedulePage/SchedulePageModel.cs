using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using Dao;
using Model.Models;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl.SchedulePage
{
    public class SchedulePageModel : AbstractModel
    {
        public static readonly string SELECTED_LESSON = "SelectedLesson";
        private LessonModel _selectedLesson;

        public class CommandHandler : ICommand
        {
            private readonly Action _action;

            public CommandHandler(Action action)
            {
                this._action = action;
            }

            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter)
            {
                _action();
            }

            public event EventHandler CanExecuteChanged;
        }

        public class ScheduleComboboxItem : ScheduleModel
        {
            public override string ToString()
            {
                return "--Пусто--";
            }
        }
        public SchedulePageModel(string id) : base(id)
        {
            GeneralDbContext.GetInstance().StreamModels.Load();
            var streamModels = new List<StreamModel>(GeneralDbContext.GetInstance().StreamModels);
            streamModels.Insert(0, new StreamModel { name = "---Пусто---", id = -1 }); // default value);
            Streams = streamModels;
            var schedules = new List<ScheduleModel>(GeneralDbContext.GetInstance().ScheduleModels);
            schedules.Insert(0, new ScheduleComboboxItem { id = -1 });
            Schedules = schedules;
            var lessonTypeModels = new List<LessonTypeModel>(GeneralDbContext.GetInstance().LessonTypeModels);
            lessonTypeModels.Insert(0, new LessonTypeModel { name = "--Пусто--", id = -1 });
            LessonTypes = lessonTypeModels;
            Show = new CommandHandler(() =>
            {
                Items.Clear();
                foreach (var model in BuildQuery())
                {
                    Items.Add(model);
                }
            });
        }


        /// <summary>
        /// SQLite EF6 provider does not support TruncateTime and other functions.
        /// </summary>
        private IEnumerable<LessonModel> BuildQuery()
        {
            var query = GeneralDbContext.GetInstance()
                .LessonModels.Where(model =>
                    model.DATE != null);
            if (SelectedStream != null && SelectedStream.id != -1)
            {
                query = query.Where(model => model.stream_id == SelectedStream.id);
            }
            if (SelectedSchedule != null && SelectedSchedule.id != -1)
            {
                query = query.Where(model => model.SCHEDULE_ID == SelectedSchedule.id);
            }
            if (SelectedLessonType != null && SelectedLessonType.id != -1)
            {
                query = query.Where(model => model.type_id == SelectedLessonType.id);
            }
            return query.AsEnumerable().Where(model => model.Date >= DateFrom && model.Date <= DateUntil);
        }

        public ObservableCollection<LessonModel> Items { get; set; } = new ObservableCollection<LessonModel>();

        public List<StreamModel> Streams { get; set; }

        public LessonModel SelectedLesson
        {
            get => _selectedLesson;
            set
            {
                _selectedLesson = value;
                string id = "11";
                var tab = new Tab
                {
                    Id = id,
                    Component = new RegistrationPage.RegistrationPage(id),
                    Header = "Регистрация"
                };
                var tabs = _store.GetState().GetOrDefault<ObservableCollection<Tab>>(TabManagerModel.TABS);
                tabs.Add(tab);
                Publisher.Publish(id + "." + SELECTED_LESSON, value);
                Publisher.Publish(TabManagerModel.TABS, new ObservableCollection<Tab>(tabs));
                Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
            }
        }

        public ICommand Show { get; set; }

        public StreamModel SelectedStream { get; set; }

        public List<ScheduleModel> Schedules { get; set; }

        public ScheduleModel SelectedSchedule { get; set; }

        public List<LessonTypeModel> LessonTypes { get; set; }

        public LessonTypeModel SelectedLessonType { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Today;

        public DateTime DateUntil { get; set; } = DateTime.Today;
    }
}
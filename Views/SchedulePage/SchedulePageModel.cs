using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Dao;
using Model.Models;
using TeacherAssistant.State;

namespace TeacherAssistant.ComponentsImpl.SchedulePage
{
    public class SchedulePageModel : AbstractModel
    {
        public class LessonView : LessonModel
        {
            private string _groupsText = null;

            public LessonView(LessonModel model)
            {
                id = model.id;
                Checked = model.Checked;
                stream_id = model.stream_id;
                Stream = model.Stream;
                group_id = model.group_id;
                Group = model.Group;
                DATE = model.DATE;
                LessonType = model.LessonType;
                SCHEDULE_ID = model.SCHEDULE_ID;
                Schedule = model.Schedule;
                description = model.description;
                create_date = model.create_date;
            }

            public string GroupsText
            {
                get
                {
                    return _groupsText
                           ?? (_groupsText =
                               group_id.HasValue
                                   ? Group.name
                                   : string.Join("\t", Stream.Groups.Select(model => model.name)));
                }
            }
        }

        public static readonly string SELECTED_LESSON = "SelectedLesson";
        private LessonModel _selectedLesson;

        private Random _random = new Random();
        private List<GroupModel> _groups;
        private ListCollectionView _lessons = new ListCollectionView(new List<LessonModel>());

        public class ScheduleComboboxItem : ScheduleModel
        {
            public override string ToString()
            {
                return "--Пусто--";
            }
        }

        /// <summary>
        /// SQLite EF6 provider does not support TruncateTime and other functions.
        /// </summary>
        private IEnumerable<LessonModel> BuildQuery()
        {
            var query = GeneralDbContext.Instance
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

            if (SelectedGroup != null && SelectedGroup.id != -1)
            {
                query = query.Where(model =>
                    model.group_id.HasValue
                        ? model.group_id.Value == SelectedGroup.id
                        : model.stream_id.HasValue
                          && model.Stream.Groups.Any(groupModel => groupModel.id == SelectedGroup.id)
                );
            }

            return query.AsEnumerable().Where(model => model.Date >= DateFrom && model.Date <= DateUntil);
        }

        public ListCollectionView Lessons
        {
            get => _lessons;
            set
            {
                _lessons = value;
                OnPropertyChanged(nameof(Lessons));
            }
        }

        public List<StreamModel> Streams { get; set; }

        public LessonModel SelectedLesson
        {
            get => _selectedLesson;
            set
            {
                _selectedLesson = value;
                var id = _random.Next(9999).ToString();
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

        public List<GroupModel> Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        public GroupModel SelectedGroup { get; set; }
        public StreamModel SelectedStream { get; set; }

        public List<ScheduleModel> Schedules { get; set; }

        public ScheduleModel SelectedSchedule { get; set; }

        public List<LessonTypeModel> LessonTypes { get; set; }

        public LessonTypeModel SelectedLessonType { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Today;

        public DateTime DateUntil { get; set; } = DateTime.Today;

        public override async Task Init(string id)
        {
            await GeneralDbContext.Instance.StreamModels.LoadAsync();
            await GeneralDbContext.Instance.GroupModels.LoadAsync();
            var streamModels = new List<StreamModel>(GeneralDbContext.Instance.StreamModels);
            streamModels.Insert(0, new StreamModel {name = "---Пусто---", id = -1}); // default value);
            Streams = streamModels;
            var schedules = new List<ScheduleModel>(GeneralDbContext.Instance.ScheduleModels);
            schedules.Insert(0, new ScheduleComboboxItem {id = -1});
            Schedules = schedules;
            var lessonTypeModels = new List<LessonTypeModel>(GeneralDbContext.Instance.LessonTypeModels);
            lessonTypeModels.Insert(0, new LessonTypeModel {name = "--Пусто--", id = -1});
            LessonTypes = lessonTypeModels;
            Groups = new List<GroupModel>(GeneralDbContext.Instance.GroupModels.Local);
            Groups.Insert(0, new GroupModel {id = -1, name = "--Пусто--"});
            Show = new CommandHandler(() =>
            {
                Lessons = new ListCollectionView(
                    new List<LessonView>(BuildQuery().Select(model => new LessonView(model))))
                {
                    CustomSort = new LessonDefaultComparerDesc()
                };
            });
        }
    }
}
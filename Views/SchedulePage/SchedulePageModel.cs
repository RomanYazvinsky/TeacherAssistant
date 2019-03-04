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
        private ObservableCollection<LessonModel> _items = new ObservableCollection<LessonModel>();
        private LessonModel _selectedLesson;
        private List<StreamModel> _streams;
        private ICommand _show;

        public class CommandHandler: ICommand
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
        public SchedulePageModel(string id) : base(id)
        {
            GeneralDbContext.GetInstance().StreamModels.Load();
            Streams = new List<StreamModel>(GeneralDbContext.GetInstance().StreamModels);
            _show = new CommandHandler(() =>
            {
                Items.Clear();
                foreach (var model in from model in GeneralDbContext.GetInstance().LessonModels
                                      where model.stream_id == SelectedStream.id
                                      select model)
                {
                    Items.Add(model);
                }
            });
        }

        public ObservableCollection<LessonModel> Items
        {
            get => _items;
            set => _items = value;
        }

        public List<StreamModel> Streams
        {
            get => _streams;
            set => _streams = value;
        }

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

        public ICommand Show
        {
            get => _show;
            set => _show = value;
        }

        public StreamModel SelectedStream { get; set; }
    }
}
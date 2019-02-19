using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Reactive.Linq;
using Dao;
using Model.Models;

namespace TeacherAssistant.State
{
    public class SideEffectManager
    {
        private static
            Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
        public static void AddSideEffect<T>(string effectId, string onId, Action<T> effect)
        {
            var selector = new Subscriber<T>(onId, effect);
            var subscription = DataExchangeManagement.GetInstance().PublishedDataStore.
                DistinctUntilChanged(selector.Get).Subscribe(selector.Run);
            _subscriptions.Add(effectId, subscription);
        }

        public static void RemoveSideEffect(string effectId)
        {
            if (!_subscriptions.ContainsKey(effectId)) return;
            _subscriptions[effectId].Dispose();
            _subscriptions.Remove(effectId);
        }

        public static void Init()
        {
            AddSideEffect<string>("OnDataBaseChange", "DataBase", newDataBase =>
            {
                if (newDataBase != null)
                {
                    GeneralDbContext.GetInstance(newDataBase).StudentModels.Load();
                    GeneralDbContext.GetInstance().GroupModels.Load();
                    GeneralDbContext.GetInstance().LessonModels.Load();
                    DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish
                    {
                        Id = "LessonsList.Items",
                        Data = new ObservableCollection<LessonModel>(GeneralDbContext.GetInstance().
                            LessonModels.Include(model => model.Schedule).
                            Include(model => model.Stream))
                    });
                    Publisher.Publish("cm.Items", new ObservableCollection<StudentGroupModel>(
                            GeneralDbContext.GetInstance()
                                .StudentGroupModels
                                .Include(model => model.Group)
                                .Include(model => model.Student
                                )
                        )
                    );
                }
            });
        }

    }

}
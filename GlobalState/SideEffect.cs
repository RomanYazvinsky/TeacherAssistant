using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Dao;
using Model.Models;

namespace Views
{
    public class SideEffect
    {
        private static
            Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
        public static void AddSideEffect<T>(string effectId, string onId, Action<T> effect)
        {
            var selector = new StoreSelector<T>(onId, effect);
            var subscription = DataExchangeManagement.GetInstance().PublishedDataStore.
                DistinctUntilChanged(selector.Get).
                Subscribe(selector.Run);
            _subscriptions.Add(effectId, subscription);
        }

        public static void RemoveSideEffect(string effectId)
        {
            _subscriptions[effectId].Dispose();
            _subscriptions.Remove(effectId);
        }

        public static void Init()
        {
            AddSideEffect<string>("OnDataBaseChange", "DataBase", newDataBase =>
            {
                if (newDataBase != null)
                {
                    GeneralDbContext.GetInstance(newDataBase);
                }
            });
            AddSideEffect<StudentModel>("OnSelectedStudentChangeUpdatePhoto", "SelectedStudent", async student =>
            {
                if (student == null) return;
                string path = await PhotoService.DownloadPhoto(student.card_id);

                DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(
                    new DataExchangeManagement.Publish
                    { Id = "PhotoPath", Data = path });
            });
            AddSideEffect<GroupModel>("OnSelectedGroupChangeUpdateTable", "SelectedGroup", newSelectedGroup =>
            {
                if (newSelectedGroup == null)
                {
                    return;
                }

                DataExchangeManagement.GetInstance().PublishedDataStore.Dispatch(new DataExchangeManagement.Publish
                {
                    Id = "SelectedGroupList",
                    Data = new ObservableCollection<StudentModel>(GeneralDbContext.GetInstance().StudentGroupModels
                        .Where(model => model.group_id == newSelectedGroup.id)
                        .Select(model => model.Student)
                    )
                });
            });
        }

    }

}
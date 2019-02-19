using Model.Models;
using Redux;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Views
{
    public class DataExchangeManagement
    {
        private static DataExchangeManagement _instance;

        public class DataContainer
        {
            private object _data;

            public DataContainer(object data)
            {
                _data = data;
            }

            public T GetData<T>()
            {
                return (T)_data;
            }
        }
        public IStore<ImmutableDictionary<string, DataContainer>> PublishedDataStore { get; }

        public class Publish : IAction
        {
            public string Id { get; set; }
            public dynamic Data { get; set; }
        }

        public class AddEffect : IAction
        {
            public string Id { get; set; }
            public string OnData { get; set; }
            public Action Effect { get; set; }
        }

        private DataExchangeManagement()
        {
            PublishedDataStore =
                new Store<ImmutableDictionary<string, DataContainer>>((state, action) =>
                    {
                        switch (action)
                        {
                            case Publish publishAction:
                                {
                                    if (publishAction.Id == null)
                                    {
                                        return state;
                                    }

                                    if (!(publishAction.Data is DataContainer))
                                    {
                                        publishAction.Data = new DataContainer(publishAction.Data);
                                    }
                                    if (!state.TryGetKey(publishAction.Id, out _))
                                    {
                                        return state.Add(publishAction.Id, publishAction.Data);
                                    }

                                    var newState = state.SetItem(publishAction.Id, publishAction.Data);

                                    return newState;
                                }
                            default:
                                {
                                    return state;
                                }
                        }
                    },
                    new Dictionary<string, DataContainer>().ToImmutableDictionary());
        }

        public static DataExchangeManagement GetInstance()
        {
            return _instance ?? (_instance = new DataExchangeManagement());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Controls;
using Redux;
using TeacherAssistant.Components;

namespace TeacherAssistant.State
{
    public class LayoutStateManagement
    {
        public IStore<ImmutableDictionary<string, ViewComponent>> AttachedPluginStore { get; set; }
        private static LayoutStateManagement _instance;

        public class AttachGenericView : AttachView
        {
            public Type GenericType { get; }
            public AttachGenericView(string id, string componentType, Grid parent, ViewConfig config, Type genericType)
                : base(id, componentType, parent, config)
            {
                GenericType = genericType;
            }
        }
        public class AttachView : IAction
        {
            public string Id { get; }
            public string ComponentType { get; set; }
            public Grid Parent { get; }
            public ViewConfig Config { get; }

            public AttachView(string id, string componentType, Grid parent, ViewConfig config)
            {
                Id = id;
                ComponentType = componentType;
                Parent = parent;
                Config = config;
            }
        }
        public class DetachView : IAction
        {
            public string Id { get; }
            public DetachView(string id)
            {
                Id = id;
            }
        }

        public class DetachAll : IAction
        {
        }

        public class Refresh : IAction
        {
            public string Id { get; set; }
        }


        public class RefreshAll : IAction
        {
        }


        public class Hide : IAction
        {
            public string Id { get; set; }
        }

        public class HideAll : IAction
        {
        }


        public class Show : IAction
        {
            public string Id { get; set; }
        }

        public class InitLayout : IAction
        {
            public int Rows { get; }
            public int Columns { get; }
            public Grid Layout { get; }

            public InitLayout(int rows, int columns, Grid layout)
            {
                Rows = rows;
                Columns = columns;
                Layout = layout;
            }
        }

        private LayoutStateManagement()
        {
            AttachedPluginStore =
                new Store<ImmutableDictionary<string, ViewComponent>>(AttachedViewComponentsReducer.Execute,
                    new Dictionary<string, ViewComponent>().ToImmutableDictionary());
        }

        public static LayoutStateManagement GetInstance()
        {
            return _instance ?? (_instance = new LayoutStateManagement());
        }
    }
}
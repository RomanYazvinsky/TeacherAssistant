using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Controls;
using Redux;

namespace Views
{
    public class LayoutStateManagement
    {
        public IStore<ImmutableDictionary<string, ViewComponent>> AttachedPluginStore { get; set; }
        private static LayoutStateManagement _instance;
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

        public class Refresh : IAction
        {
            public string Id { get; set; }
        }


        public class RefreshAll : IAction
        {
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
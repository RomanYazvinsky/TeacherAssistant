using System;
using System.Windows.Controls;

namespace TeacherAssistant.Components
{
    public class ViewComponent
    {
        public string Id { get; set; }
        public string ComponentType { get; set; }
        public UserControl Layout { get; set; }
        public Grid Parent { get; set; }
        public ViewConfig Config { get; set; }
        public Type DataType { get; protected set; }

        public ViewComponent(string id, string componentType, Grid parent, ViewConfig config)
        {
            Id = id;
            ComponentType = componentType;
            Parent = parent;
            Config = config;
        }
        public ViewComponent(string id, string componentType, Grid parent, ViewConfig config, Type dataType)
            : this(id, componentType, parent, config)
        {
            DataType = dataType;
        }

        public void Render(UserControl layout)
        {
            if (layout == null)
            {
                return;
            }

            if (Layout != null)
            {
                Remove();
            }

            Layout = layout;
            Parent.Children.Add(Layout);
            Grid.SetColumn(Layout, Config.Column);
            Grid.SetRow(Layout, Config.Row);
            if (Config.ColumnSize != 0)
            {
                Grid.SetColumnSpan(Layout, Config.ColumnSize);
            }

            if (Config.RowSize != 0)
            {
                Grid.SetRowSpan(Layout, Config.RowSize);
            }
        }

        public void Remove()
        {
            ((IDisposable)Layout.DataContext)?.Dispose();
            Parent.Children.Remove(Layout);
        }
    }
}
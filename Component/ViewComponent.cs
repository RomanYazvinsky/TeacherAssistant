using System.Windows.Controls;

namespace Views
{
    public class ViewComponent
    {
        public string Id { get; set; }
        public string ComponentType { get; set; }
        public UserControl Layout { get; set; }
        public Grid Parent { get; set; }
        public ViewConfig Config { get; set; }

        public ViewComponent(string id, string componentType, Grid parent, ViewConfig config)
        {
            Id = id;
            ComponentType = componentType;
            Parent = parent;
            Config = config;
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
            Grid.SetColumnSpan(Layout, Config.ColumnSize);
            Grid.SetRowSpan(Layout, Config.RowSize);
        }

        public void Remove()
        {
            Parent.Children.Remove(Layout);
        }
    }
}
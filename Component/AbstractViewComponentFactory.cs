using System.Windows.Controls;

namespace Views
{
    public abstract class AbstractViewComponentFactory
    {
        public string ComponentType { get; set; }

        public ViewComponent Build(string id, Grid parent, ViewConfig config)
        {
            return new ViewComponent(id, ComponentType, parent, config);
        }

        public ViewComponent Attach(string id, Grid parent, ViewConfig config)
        {
            var viewComponent = Build(id, parent, config);
            viewComponent.Render(GetLayout(id));
            return viewComponent;
        }

        public abstract UserControl GetLayout(string id);
    }
}
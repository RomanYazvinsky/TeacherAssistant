using System;
using System.Windows.Controls;

namespace TeacherAssistant.Components
{
    public abstract class GenericViewComponentFactory
    {
        public string ComponentType { get; set; }


        public ViewComponent Build(string id, Grid parent, ViewConfig config, Type dataType)
        {
            return new ViewComponent(id, ComponentType, parent, config, dataType);
        }

        public ViewComponent Attach(string id, Grid parent, ViewConfig config, Type dataType)
        {
            var viewComponent = Build(id, parent, config, dataType);
            viewComponent.Render(GetGenericLayout(id, dataType));
            return viewComponent;
        }

        public UserControl GetGenericLayout(string id, Type dataType)
        {
            return (UserControl)GetType().GetMethod(nameof(GetLayout)).MakeGenericMethod(dataType).Invoke(this, new object[] { id });
        }

        public abstract UserControl GetLayout<T>(string id);
    }
}
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using Redux;
using Views.StudentList;

namespace Views
{
    public static class AttachedViewComponentsReducer
    {
        public static ImmutableDictionary<string, ViewComponent> Execute(
            ImmutableDictionary<string, ViewComponent> state, IAction action)
        {
            switch (action)
            {
                case LayoutStateManagement.RefreshAll refreshAll:
                    {

                        var factories = ViewComponentFactoriesModule.GetInstance().ViewComponentFactories;
                        foreach (var data in state)
                        {
                            var component = data.Value;
                            component.Render(
                                factories.First(factory => factory.ComponentType.Equals(component.ComponentType))
                                    .GetLayout(component.Id)
                                );
                        }
                        return state;
                    }
                case LayoutStateManagement.InitLayout initLayout:
                    {
                        var layout = initLayout.Layout;
                        layout.ColumnDefinitions.Clear();
                        layout.RowDefinitions.Clear();
                        for (int i = 0; i < initLayout.Columns; i++)
                        {
                            layout.ColumnDefinitions.Add(new ColumnDefinition());
                        }

                        for (int i = 0; i < initLayout.Rows; i++)
                        {
                            layout.RowDefinitions.Add(new RowDefinition());
                        }

                        return state;
                    }
                case LayoutStateManagement.AttachView attachComponent:
                    {
                        var factories = ViewComponentFactoriesModule.GetInstance().ViewComponentFactories;
                        var factory = factories.First(component => component.ComponentType.Equals(attachComponent.ComponentType));
                        
                        return state.Add(attachComponent.Id, factory.Attach(attachComponent.Id, attachComponent.Parent, attachComponent.Config));
                    }
                default:
                    return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
            }
        }
    }
}
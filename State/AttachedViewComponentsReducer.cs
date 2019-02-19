using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Model.Models;
using Redux;
using TeacherAssistant.Components;

namespace TeacherAssistant.State
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

                        var componentFactories = ViewComponentFactoriesModule.GetInstance().ViewComponentFactories;
                        var genericFactories = ViewComponentFactoriesModule.GetInstance().GenericViewComponentFactories;
                        foreach (var data in state)
                        {
                            var component = data.Value;
                            if (componentFactories.Any(factory => factory.ComponentType.Equals(component.ComponentType)))
                            {
                                component.Render(
                                    componentFactories.First(factory => factory.ComponentType.Equals(component.ComponentType))
                                        .GetLayout(component.Id)
                                );
                            }
                            else
                            {
                                component.Render(
                                    genericFactories.First(factory => factory.ComponentType.Equals(component.ComponentType))
                                        .GetGenericLayout(component.Id, component.DataType)
                                );
                            }

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
                case LayoutStateManagement.AttachGenericView attachGenericView:
                    {
                        if (state.ContainsKey(attachGenericView.Id))
                        {
                            state[attachGenericView.Id].Layout.Visibility = Visibility.Visible;

                            return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
                        }
                        var factories = ViewComponentFactoriesModule.GetInstance().GenericViewComponentFactories;
                        var factory = factories.First(component => component.ComponentType.Equals(attachGenericView.ComponentType));

                        return state.Add(
                            attachGenericView.Id,
                            factory.Attach
                            (
                                attachGenericView.Id,
                                attachGenericView.Parent,
                                attachGenericView.Config,
                                attachGenericView.GenericType
                            )
                        );
                    }
                case LayoutStateManagement.AttachView attachComponent:
                    {
                        if (state.ContainsKey(attachComponent.Id))
                        {
                            state[attachComponent.Id].Layout.Visibility = Visibility.Visible;
                            return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
                        }
                        var factories = ViewComponentFactoriesModule.GetInstance().ViewComponentFactories;
                        var factory = factories.First(component => component.ComponentType.Equals(attachComponent.ComponentType));

                        return state.Add(attachComponent.Id, factory.Attach(attachComponent.Id, attachComponent.Parent, attachComponent.Config));
                    }
                case LayoutStateManagement.DetachView detachView:
                    {
                        state[detachView.Id].Remove();
                        return state.Remove(detachView.Id);
                    }
                case LayoutStateManagement.DetachAll detachAll:
                    {
                        foreach (var viewComponent in state)
                        {
                            viewComponent.Value.Remove();
                        }
                        return new Dictionary<string, ViewComponent>().ToImmutableDictionary();
                    }
                case LayoutStateManagement.HideAll hideAll:
                    {
                        foreach (var pair in state)
                        {
                            pair.Value.Layout.Visibility = Visibility.Collapsed;
                        }
                        return new Dictionary<string, ViewComponent>().ToImmutableDictionary();
                    }
                case LayoutStateManagement.Hide hide:
                    {
                        if (!state.ContainsKey(hide.Id))
                        {
                            return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
                        }
                        var viewComponent = state[hide.Id];
                        viewComponent.Layout.Visibility = Visibility.Collapsed;
                        return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
                    }
                case LayoutStateManagement.Show show:
                    {
                        var viewComponent = state[show.Id];
                        viewComponent.Layout.Visibility = Visibility.Visible;
                        return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
                    }

                default:
                    return new Dictionary<string, ViewComponent>(state).ToImmutableDictionary();
            }
        }
    }
}
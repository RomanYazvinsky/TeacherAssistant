using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.Table
{

    public class GenericTableControl
    {
        public class SortAdorner : Adorner
        {
            private static Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            public ListSortDirection Direction { get; private set; }

            public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
            {
                this.Direction = dir;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                TranslateTransform transform = new TranslateTransform
                (
                    AdornedElement.RenderSize.Width - 15,
                    (AdornedElement.RenderSize.Height - 5) / 2
                );
                drawingContext.PushTransform(transform);

                Geometry geometry = ascGeometry;
                if (this.Direction == ListSortDirection.Descending)
                    geometry = descGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
        }

        public static UserControl<T> Build<T>(string id)
        {
            var state = DataExchangeManagement.GetInstance().PublishedDataStore.GetState();
            var config = Publisher.Get<TableConfig<T>>(state, id + ".TableConfig");
            var control = new UserControl<T>();
            var model = new TableModel<T>(id);
            control.DataContext = model;
            control.HorizontalAlignment = HorizontalAlignment.Stretch;
            control.VerticalAlignment = VerticalAlignment.Stretch;
            var grid = new Grid();
            var listView = new ListView();
            var columnGrid = new GridView();
            control.Content = grid;
            listView.SelectionMode = SelectionMode.Single;
            listView.VerticalAlignment = VerticalAlignment.Stretch;
            listView.HorizontalAlignment = HorizontalAlignment.Stretch;
            listView.View = columnGrid;
            
            grid.Children.Add(listView);
            Binding items = new Binding("Items")
            {
                Source = model
            };
            listView.SetBinding(ItemsControl.ItemsSourceProperty, items);
            if (config.EnableMultiSelect)
            {
                listView.SelectionMode = SelectionMode.Extended;
                listView.SelectionChanged += (sender, args) =>
                {
                    model.SelectedItems = new List<T>();
                    foreach (var listViewSelectedItem in listView.SelectedItems)
                    {
                        model.SelectedItems.Add(listViewSelectedItem);
                    }

                };
            }
            ApplyColumnConfig(id, config, model, listView);
            return control;
        }


        private static void ApplyColumnConfig<T>(string id, TableConfig<T> config, TableModel<T> model, ListView listView)
        {
            var select = new Action(() =>
            {
                if (listView.SelectedItem != null)
                {
                    DataExchangeManagement.GetInstance().PublishedDataStore
                        .Dispatch(new DataExchangeManagement.Publish
                        { Id = id + ".SelectedItem", Data = listView.SelectedItem });
                }
            });
            if (config.SelectOnClick)
            {
                Binding selectedItem = new Binding("SelectedItem")
                {
                    Source = model
                };
                listView.SetBinding(Selector.SelectedItemProperty, selectedItem);
            }

            if (config.SelectOnDoubleClick)
            {
                listView.MouseDoubleClick += (sender, args) => select();
            }

            config.DefaultSortColumn?.SortOrder.ForEach(listView.Items.SortDescriptions.Add);

            if (config.SelectOnEnter)
            {
                listView.KeyDown += (sender, args) =>
                {
                    if (args.Key == Key.Return)
                    {
                        select();
                    }
                };
            }

            GridView gridView = (GridView)listView.View;

            GridViewColumnHeader listViewSortCol = null;
            SortAdorner listViewSortAdorner = null;
            var sortFunction = new RoutedEventHandler((sender, args) =>
            {
                GridViewColumnHeader column = (sender as GridViewColumnHeader);
                string sortBy = column.Tag.ToString();
                if (listViewSortCol != null)
                {
                    AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                    listView.Items.SortDescriptions.RemoveAt(0);
                }

                ListSortDirection newDir = ListSortDirection.Ascending;
                if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                    newDir = ListSortDirection.Descending;

                listViewSortCol = column;
                listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
                AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                listView.Items.SortDescriptions.Insert(0, new SortDescription(sortBy, newDir));
            });
            foreach (var columnBinding in config.ColumnConfigs)
            {
                var settings = columnBinding.Value;
                var gridViewColumnHeader = new GridViewColumnHeader
                {
                    Content = columnBinding.Key,
                    Tag = settings.PropertyPath
                };
                var gridViewColumn = new GridViewColumn
                {
                    DisplayMemberBinding = new Binding(settings.PropertyPath),
                    Header = gridViewColumnHeader
                };
                gridView.Columns.Add(gridViewColumn);
                if (settings.StringFormat != null)
                {
                    gridViewColumn.DisplayMemberBinding.StringFormat = settings.StringFormat;
                }
                if (settings.DefaultWidth != 0.0D)
                {
                    gridViewColumn.Width = settings.DefaultWidth;
                }

                if (settings.SortEnabled)
                {
                    ((GridViewColumnHeader)gridViewColumn.Header).Click += sortFunction;
                }
            }
            /* return new DataStoreSelector<TableColumnConfig>(id + ".ColumnConfig", config =>
           {
               GridViewColumnHeader listViewSortCol = null;
               SortAdorner listViewSortAdorner = null;
               var sortFunction = new RoutedEventHandler((sender, args) =>
               {
                   GridViewColumnHeader column = (sender as GridViewColumnHeader);
                   string sortBy = column.Tag.ToString();
                   if (listViewSortCol != null)
                   {
                       AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                       listView.Items.SortDescriptions.Clear();
                   }

                   ListSortDirection newDir = ListSortDirection.Ascending;
                   if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                       newDir = ListSortDirection.Descending;

                   listViewSortCol = column;
                   listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
                   AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                   listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
               });
               foreach (var columnBinding in config.ColumnConfigs)
               {
                   var settings = columnBinding.Value;
                   var gridViewColumnHeader = new GridViewColumnHeader
                   {
                       Content = columnBinding.Key,
                       Tag = settings.PropertyPath
                   };
                   var gridViewColumn = new GridViewColumn
                   {
                       DisplayMemberBinding = new Binding(settings.PropertyPath),
                       Header = gridViewColumnHeader
                   };
                   gridView.Columns.Add(gridViewColumn);
                   if (settings.StringFormat != null)
                   {
                       gridViewColumn.DisplayMemberBinding.StringFormat = settings.StringFormat;
                   }
                   if (settings.DefaultWidth != 0.0D)
                   {
                       gridViewColumn.Width = settings.DefaultWidth;
                   }

                   if (settings.SortEnabled)
                   {
                       ((GridViewColumnHeader)gridViewColumn.Header).Click += sortFunction;
                   }
               }

               if (config.DefaultSortColumn != null)
               {
                   listView.Items.SortDescriptions.Add(config.DefaultSortColumn.Value);
               }
           }).SubscribeOnChanges();*/
        }

    }
}
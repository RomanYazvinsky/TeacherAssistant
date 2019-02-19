using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TeacherAssistant.State;

namespace TeacherAssistant.Components.Combobox
{
    public class GenericComboboxControl
    {
        public static UserControl<T> Build<T>(string id, bool selectedItemBinding)
        {
            var control = new UserControl<T>();
            var model = new ComboboxModel<T>(id);
            control.DataContext = model;
            control.HorizontalAlignment = HorizontalAlignment.Stretch;
            control.VerticalAlignment = VerticalAlignment.Stretch;
            var grid = new Grid();
            var comboBox = new ComboBox();
            control.Content = grid;
            comboBox.VerticalAlignment = VerticalAlignment.Stretch;
            comboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.Children.Add(comboBox);
            Binding items = new Binding("Items")
            {
                Source = model
            };
            comboBox.SetBinding(ItemsControl.ItemsSourceProperty, items);
            if (selectedItemBinding)
            {
                Binding selectedItem = new Binding("SelectedItem")
                {
                    Source = model
                };
                comboBox.SetBinding(Selector.SelectedItemProperty, selectedItem);
            }
            ApplyComboboxConfig(id, comboBox);
            return control;
        }

        private static IDisposable ApplyComboboxConfig(string id, ComboBox comboBox)
        {
            return new Subscriber<ComboboxConfig>(id + ".ComboboxConfig", config =>
            {
                if (config != null)
                {
                    comboBox.DisplayMemberPath = config.DisplayProperty;
                    comboBox.MaxHeight = config.MaxHeight;
                }
            }).SubscribeOnChanges();
        }
    }
}
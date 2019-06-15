using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Footer.TaskExpandList
{
    public class TaskExpandListModel : AbstractModel
    {
        private bool _expanded;
        private List<MenuItem> _items = new List<MenuItem>();

        public List<MenuItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public override async Task Init(string id)
        {
            _id = id;
            SimpleSubscribeCollection<TaskHandler>("TaskList", models =>
            {
                if (models == null) return;
                Items = new List<MenuItem>(models.Select(model =>
                {
                    var item = new MenuItem();
                    var itemHeader = new Grid();
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(100, GridUnitType.Pixel) });
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(100, GridUnitType.Pixel) });
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
                    item.Header = itemHeader;
                    var textBlock = new TextBlock() { Text = model.Name };
                    itemHeader.Children.Add(textBlock);
                    var bar = new ProgressBar
                    { Maximum = model.Maximum, Minimum = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
                    itemHeader.Children.Add(bar);
                    var cancel = new Button
                    {
                        Command = new CommandHandler(model.Cancel),
                        Content = "X",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Width = 25
                    };
                    itemHeader.Children.Add(cancel);
                    Grid.SetColumn(textBlock, 0);
                    Grid.SetColumn(bar, 1);
                    Grid.SetColumn(cancel, 2);
                    model.Value.Subscribe(i => Application.Current.Dispatcher.Invoke(() => bar.Value = i));
                    model.Value.LastAsync().Delay(TimeSpan.FromMilliseconds(1500)).Subscribe((i) =>
                    {
                        Publisher.Remove("TaskList", model);
                    });
                    return item;
                }));
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant.Footer.TaskExpandList
{
    public class TaskExpandListModel : AbstractModel
    {
        private static readonly string LocalizationKey = "tasks";
        [Reactive] public List<MenuItem> Items { get; set; }
        [Reactive] public bool IsExpanded { get; set; }
        [Reactive] public Visibility IsVisible { get; set; } = Visibility.Collapsed;
        [Reactive] public int MainValue { get; set; }
        [Reactive] public int MainMaxValue { get; set; }
        [Reactive] public bool IsIndeterminate { get; set; }
        [Reactive] public CommandHandler Cancel { get; set; }
        [Reactive] public Visibility IsButtonVisible { get; set; } = Visibility.Hidden;

        public TaskExpandListModel(string id): base(id)
        {
            this.WhenAnyValue(model => model.Items).Subscribe(items =>
            {
                if (items == null)
                {
                    this.IsVisible = Visibility.Collapsed;
                    return;
                }

                this.IsVisible = items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                this.IsButtonVisible = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        protected override string GetLocalizationKey()
        {
            return LocalizationKey;
        }

        public override Task Init()
        {
            SelectCollection<TaskHandler>("TaskList").Subscribe(models =>
            {
                if (models == null) return;
                this.Items = new List<MenuItem>(models.Select(model =>
                {
                    var item = new MenuItem();
                    var itemHeader = new Grid();
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition
                                                     {Width = new GridLength(100, GridUnitType.Pixel)});
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition
                                                     {Width = new GridLength(100, GridUnitType.Pixel)});
                    itemHeader.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(25)});
                    item.Header = itemHeader;
                    var textBlock = new TextBlock() {Text = model.Name};
                    itemHeader.Children.Add(textBlock);
                    var bar = new ProgressBar
                              {
                                  Maximum = model.Maximum, Minimum = 0,
                                  HorizontalAlignment = HorizontalAlignment.Stretch,
                                  IsIndeterminate = model.IsIndeterminate
                              };
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
                    model.Value.Subscribe(i => Application.Current.Dispatcher?.Invoke(() => bar.Value = i));
                    model.Value.LastAsync().Delay(TimeSpan.FromMilliseconds(1500)).Subscribe((i) =>
                    {
                        StoreManager.Remove(
                            "TaskList", model);
                    });
                    return item;
                }));
                var mainModel = models.FirstOrDefault();
                if (mainModel == null)
                {
                    this.MainValue = 0;
                    return;
                }

                this.Cancel = new CommandHandler(() => { mainModel.Cancel(); });
                mainModel.Value.Subscribe(value => this.MainValue = value);
                this.MainMaxValue = mainModel.Maximum;
                this.IsIndeterminate = mainModel.IsIndeterminate;
                mainModel.Value.LastAsync().Delay(TimeSpan.FromMilliseconds(mainModel.IsIndeterminate ? 100 : 1500))
                         .Subscribe((i) =>
                          {
                              StoreManager.Remove(
                                  "TaskList", mainModel);
                          });
            });
            return Task.CompletedTask;
        }
    }
}
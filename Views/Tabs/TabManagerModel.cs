using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.State;

namespace TeacherAssistant
{
    public class TabManagerModel : AbstractModel
    {
        public static readonly string TABS = "Tabs";
        public static readonly string ACTIVE_TAB = "ActiveTab";
        private ObservableCollection<TabItem> _tabs = new ObservableCollection<TabItem>();
        private TabItem _activeTab;

        private TabItem GetTabItemByTab(Tab tab)
        {
            return tab == null ? null : _tabs.FirstOrDefault(item => item.Content.Equals(tab.Component));
        }

        private Tab GetTabByTabItem(TabItem tab)
        {
            return tab == null
                ? null
                : _store.GetState().GetOrDefault<ObservableCollection<Tab>>(TABS)
                    .FirstOrDefault(item => tab.Content.Equals(item.Component));
        }

        private void CloseTab(Tab tab)
        {
            var tabs = _store.GetState().GetOrDefault<ObservableCollection<Tab>>(TABS);
            var index = tabs.IndexOf(tab);
           
            bool isLast = index == tabs.Count - 1;
            if (index == 0 && isLast)
            {
                return;
            }
            tabs.Remove(tab);
            Publisher.Publish(TABS, new ObservableCollection<Tab>(tabs)); // For DistinctUntilChanged
            if (isLast)
            {
                Publisher.Publish(ACTIVE_TAB, tabs[tabs.Count - 1]);
            }
            else
            {
                Publisher.Publish(ACTIVE_TAB, GetTabByTabItem(ActiveTab));
            }
        }

        public ObservableCollection<TabItem> Tabs
        {
            get => _tabs;
            set
            {
                _tabs = value;
                OnPropertyChanged(nameof(Tabs));
            }
        }

        public TabItem ActiveTab
        {
            get => _activeTab;
            set
            {
                if (value == null || _activeTab == value)
                {
                    return;
                }

                _activeTab = value;
                value.IsSelected = true;
                Publisher.Publish(ACTIVE_TAB, GetTabByTabItem(value));
                OnPropertyChanged(nameof(ActiveTab));
            }
        }

        public override async Task Init(string id)
        {

            SimpleSubscribe<Tab>(ACTIVE_TAB, tab =>
            {
                if (tab == null) return;
                ActiveTab = GetTabItemByTab(tab);
            });
            SimpleSubscribeCollection<Tab>(TABS, tabs =>
            {
                if (tabs == null) return;
                Tabs.Clear();
                var tabItems = tabs.Select(tab =>
                {
                    var tabItem = new TabItem();
                    var header = new Grid { Width = 100 };
                    var headerName = new TextBlock { Text = tab.Header, TextTrimming = TextTrimming.CharacterEllipsis };
                    header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    header.Children.Add(headerName);
                    var closeButton = new TextBlock
                    {
                        Text = "X",
                        Width = 20,
                        TextAlignment = TextAlignment.Right
                    };
                    header.MouseEnter += (sender, args) =>
                    {
                        if (Tabs.Count <= 1)
                        {
                            return;
                        }
                        
                     
                    };
                    closeButton.MouseDown += (o, args1) => { CloseTab(tab); };
                    header.Children.Add(closeButton);
                    Grid.SetColumn(closeButton, 1);
                    //header.MouseLeave += (sender, args) =>
                    //{
                    //    if (Tabs.Count < 2 || header.Children.Count < 2)
                    //    {
                    //        return;
                    //    }

                    //    header.Children.RemoveAt(1);
                    //};
                    tabItem.Header = header;
                    tabItem.Content = tab.Component;
                    return tabItem;
                });
                foreach (var item in tabItems)
                {
                    Tabs.Add(item);
                }
            });
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using Dao;
using Ninject;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Footer.TaskExpandList;
using TeacherAssistant.Properties;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TeacherAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ISerialUtil SerialUtil { get; }
        private Random _random = new Random();

        public MainWindow(ISerialUtil serialUtil)
        {
            InitializeComponent();
            SerialUtil = serialUtil;
            SerialUtil.ConnectionFailed += (sender, s1) => MessageBox.Show(s1);
            Reader_Toggle.Header = "Подключить считыватель";
            Reader_Toggle.Command = new CommandHandler(() =>
            {
                SerialUtil.Start();
            });
            SerialUtil.Connected += (sender, s1) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Reader_Toggle.Header = "Отключить считыватель";
                    Reader_Toggle.Command = new CommandHandler(() => { SerialUtil.Close(); });
                });
            };
            SerialUtil.Disconnected += (sender, s1) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Reader_Toggle.Header = "Подключить считыватель";
                    Reader_Toggle.Command = new CommandHandler(() => { SerialUtil.Start(); });
                });
            };
            var tabManagerModel = new TabManagerModel();
            tabManagerModel.Init("");
            DataContext = tabManagerModel;
            GeneralDbContext.DatabaseChanged += (sender, args) =>
            {
                Settings.Default.DatabasePath = args;
                Settings.Default.Save();
                var tabs = new ObservableCollection<Tab>();
                var id = _random.Next(9999).ToString();
                var tab = new Tab
                {
                    Id = id,
                    Header = "Расписание",
                    Component = new SchedulePage.SchedulePage(id)
                };
                tabs.Add(tab);
                Publisher.Publish(TabManagerModel.TABS, tabs);
                Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
            };
            if (File.Exists(Settings.Default.DatabasePath))
            {
                GeneralDbContext.Reconnect(Settings.Default.DatabasePath);
            }

            var s = new StatusBarItem();
            s.Content = new TaskExpandList();
            s.Width = 100;
            Publisher.Add("StatusItems", s);
        }

        private void SelectDatabase_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            bool? result = dlg.ShowDialog();

            if (result != true) return;
            GeneralDbContext.Reconnect(dlg.FileName);
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            var tabs = Publisher.Get<ObservableCollection<Tab>>(TabManagerModel.TABS);
            var id = _random.Next(9999).ToString();
            var tab = new Tab
            {
                Id = id,
                Header = "Расписание",
                Component = new SchedulePage.SchedulePage(id)
            };
            tabs.Add(tab);
            Publisher.Publish(TabManagerModel.TABS, new ObservableCollection<Tab>(tabs));
            Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
        }

        private void Students_Click(object sender, RoutedEventArgs e)
        {
            var tabs = Publisher.Get<ObservableCollection<Tab>>(TabManagerModel.TABS);
            var id = _random.Next(9999).ToString();
            var tab = new Tab
            {
                Id = id,
                Header = "Студенты",
                Component = new StudentTable.StudentTable(id)
            };
            tabs.Add(tab);
            Publisher.Publish(TabManagerModel.TABS, new ObservableCollection<Tab>(tabs));
            Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
        }

        private void Groups_Click(object sender, RoutedEventArgs e)
        {
            var tabs = Publisher.Get<ObservableCollection<Tab>>(TabManagerModel.TABS);
            var id = _random.Next(9999).ToString();
            var tab = new Tab
            {
                Id = id,
                Header = "Группы",
                Component = new GroupTable.GroupTable(id)
            };
            tabs.Add(tab);
            Publisher.Publish(TabManagerModel.TABS, new ObservableCollection<Tab>(tabs));
            Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
        }

        private void SelectPhotoDir_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                DialogResult result = dlg.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                {
                    Settings.Default.PhotoDirPath = dlg.SelectedPath;
                }
            }
        }
        

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SerialUtil.Close();
        }
    }
}
﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Dao;
using Ninject;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;

namespace TeacherAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [Inject]
        public ISerialUtil SerialUtil { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new TabManagerModel();
            GeneralDbContext.DatabaseChanged += (sender, args) =>
            {
                var tabs = new ObservableCollection<Tab>();
                var tab = new Tab
                {
                    Id = "SchedulePage",
                    Header = "Расписание",
                    Component = new SchedulePage.SchedulePage("")
                };
                tabs.Add(tab);
                Publisher.Publish(TabManagerModel.TABS, tabs);
                Publisher.Publish(TabManagerModel.ACTIVE_TAB, tab);
            };
        }

        private void SelectDatabase_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            bool? result = dlg.ShowDialog();

            if (result != true) return;
            GeneralDbContext.GetInstance(dlg.FileName);
        }

        private void ConnectReader_Click(object sender, RoutedEventArgs e)
        {
            SerialUtil.Start();
        }

        private void DisconnectReader_Click(object sender, RoutedEventArgs e)
        {
            SerialUtil.Close();
        }
       

    }
}
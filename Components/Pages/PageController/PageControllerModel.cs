using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Containers;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.Tabs;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Dao;
using TeacherAssistant.Forms.GroupForm;
using TeacherAssistant.Forms.StreamForm;
using TeacherAssistant.GroupTable;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.Pages.SettingsPage;
using TeacherAssistant.Properties;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.StudentForm;
using TeacherAssistant.StudentTable;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TeacherAssistant.Pages {
    public class PageControllerModel : AbstractModel {
        private readonly MainReducer _mainReducer;
        private readonly WindowPageHost _windowPageHost;
        private SerialUtil SerialUtil { get; }
        private TabPageHost _host;

        public PageControllerModel(
            ModuleLoader loader,
            PageControllerReducer reducer,
            SerialUtil serialUtil,
            MainReducer mainReducer,
            WindowPageHost windowPageHost) {
            _mainReducer = mainReducer;
            _windowPageHost = windowPageHost;
            this.SerialUtil = serialUtil;
            InitHandlers();
            var tabControllerToken = new TabControllerToken();
            var tabControllerModule = loader.Activate(tabControllerToken);
            _host = tabControllerModule.Injector.Locate<TabPageHost>();
            this.CentralControl = tabControllerModule.GetEntryComponent();
            _host.AddPage(new ScheduleToken("Schedule"));
            reducer.Select(state => state.SelectedPage)
                .Where(NotNull)
                .WithLatestFrom(reducer.Select(state => state.Controls), ToTuple)
                .Subscribe(tuple => {
                    var (selectedPage, controlsDict) = tuple;
                    var controls = selectedPage == null ? new List<ButtonConfig>() : controlsDict[selectedPage.Id];
                    foreach (var buttonConfig in controls.Where(config => config.Icon != null)) {
                        var dependencyObject = VisualTreeHelper.GetParent(buttonConfig.Icon);
                        ((Grid) dependencyObject)?.Children.Remove(buttonConfig.Icon);
                    }

                    this.CurrentControls.Clear();
                    var c = new List<ButtonConfig>(controls);
                    c.Reverse();
                    var menuItems = c.Select(config => {
                        var header = new Grid();
                        if (config.Icon != null) {
                            header.Children.Add(config.Icon);
                        }

                        if (config.Text != null) {
                            header.Children.Add(new TextBlock {Text = config.Text, FontSize = 12});
                        }

                        var menuItem = new MenuItem {
                            Header = header,
                            Command = config.Command,
                            ToolTip = config.Tooltip,
                            HorizontalContentAlignment = HorizontalAlignment.Center
                        };
                        return menuItem;
                    }).ToList();
                    this.CurrentControls.AddRange(menuItems);
                });
        }

        private void InitHandlers() {
            this.OpenSchedule = new CommandHandler(OpenSchedulePage);
            this.OpenStudentsTable = new CommandHandler(Students_Click);
            this.OpenGroupsTable = new CommandHandler(Groups_Click);
            this.OpenSelectPhotoDirectoryDialog = new CommandHandler(SelectPhotoDir_Click);
            this.OpenSelectDatabaseDialog = new CommandHandler(SelectDatabase_Click);
            this.OpenSettings = new CommandHandler(OpenSettingsClick);
            this.OpenAddStudentForm = new CommandHandler(AddStudent_Click);
            this.OpenAddLessonForm = new CommandHandler(AddLesson_Click);
            this.OpenAddGroupForm = new CommandHandler(AddGroup_Click);
            this.OpenAddStreamForm = new CommandHandler(AddStreamHandler);
        }


        [Reactive] public Control CentralControl { get; set; }

        public CommandHandler OpenSchedule { get; set; }
        public CommandHandler OpenStudentsTable { get; set; }
        public CommandHandler OpenGroupsTable { get; set; }
        public CommandHandler OpenSelectPhotoDirectoryDialog { get; set; }
        public CommandHandler OpenSelectDatabaseDialog { get; set; }
        public CommandHandler OpenSettings { get; set; }
        public CommandHandler OpenAddStudentForm { get; set; }
        public CommandHandler OpenAddLessonForm { get; set; }
        public CommandHandler OpenAddStreamForm { get; set; }
        public CommandHandler OpenAddGroupForm { get; set; }

        public ObservableRangeCollection<MenuItem> CurrentControls { get; set; } =
            new WpfObservableRangeCollection<MenuItem>();

        private void SelectDatabase_Click() {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();

            if (result != true)
                return;

            LocalDbContext.Reconnect(dialog.FileName);

            Settings.Default.DatabasePath = dialog.FileName;
            Settings.Default.Save();
        }

        private void OpenSchedulePage() {
            _host.AddPage(new ScheduleToken("Schedule"));
        }

        private void Students_Click() {
            _host.AddPage(new StudentTableToken("Студенты"));
        }

        private void Groups_Click() {
            _host.AddPage(new GroupTableToken("Группы"));
        }

        private void SelectPhotoDir_Click() {
            using (var dlg = new FolderBrowserDialog()) {
                var result = dlg.ShowDialog();

                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPath))
                    return;
                Settings.Default.PhotoDirPath = dlg.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void AddStudent_Click() {
            _windowPageHost.AddPage(new StudentFormToken("Добавить студента", new StudentEntity()));
        }

        private void AddLesson_Click() {
            _windowPageHost.AddPage(new LessonFormToken("Добавить занятие", new LessonEntity()));
        }

        private void AddGroup_Click() {
            _windowPageHost.AddPage(new GroupFormToken("Добавить группу", new GroupEntity()));
        }

        private void AddStreamHandler() {
            _windowPageHost.AddPage(new StreamFormToken("Добавить поток", new StreamEntity()));
        }

        private void OpenSettingsClick() {
            _host.AddPage(new SettingsToken("Settings"));
        }


        public void ManageHotKeys(KeyEventArgs e) {
            if (e.Key == Key.F11) {
                _mainReducer.Dispatch(new SetFullscreenModeAction());
            }
        }

        protected override string GetLocalizationKey() {
            return "main";
        }
    }
}
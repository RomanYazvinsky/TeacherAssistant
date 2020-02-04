using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
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
using TeacherAssistant.State;
using TeacherAssistant.StudentForm;
using TeacherAssistant.StudentTable;
using App = System.Windows.Application;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TeacherAssistant.Pages
{
    public class PageControllerModel : AbstractModel
    {
        private readonly WindowPageHost _windowPageHost;
        private SerialUtil SerialUtil { get; }
        private TabPageHost _host;

        public PageControllerModel(
            ModuleActivator activator,
            PageControllerReducer reducer,
            SerialUtil serialUtil,
            MainReducer mainReducer,
            WindowPageHost windowPageHost)
        {
            _windowPageHost = windowPageHost;
            this.SerialUtil = serialUtil;
            InitHandlers();
            var tabControllerToken = new TabControllerToken();
            Observable.FromAsync(() => activator.ActivateAsync(tabControllerToken))
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(module =>
                {
                    _host = module.Injector.Locate<TabPageHost>();
                    this.CentralControl = module.GetEntryComponent();
                    _host.AddPageAsync(new ScheduleToken("Schedule"));
                });

            mainReducer.Select(state => state.FullscreenMode)
                .Subscribe(isFullScreen => { this.MenuVisibility = !isFullScreen; });
            reducer.Select(state => state.SelectedPage)
                .Where(NotNull)
                .WithLatestFrom(reducer.Select(state => state.Controls), ToTuple)
                .Subscribe(tuple =>
                {
                    var (selectedPage, controlsDict) = tuple;
                    var controls = selectedPage == null
                        ? new List<ButtonConfig>()
                        : controlsDict.GetOrDefault(selectedPage.Id) ?? new List<ButtonConfig>();
                    SetActionButtons(controls);
                });
        }

        private void InitHandlers()
        {
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

        private void SetActionButtons(List<ButtonConfig> buttons)
        {
            foreach (var buttonConfig in buttons.Where(config => config.Icon != null))
            {
                var dependencyObject = VisualTreeHelper.GetParent(buttonConfig.Icon);
                ((Grid) dependencyObject)?.Children.Remove(buttonConfig.Icon);
            }

            this.CurrentControls.Clear();
            var items = new List<ButtonConfig>(buttons);
            items.Reverse();
            var menuItems = items.Select(BuildMenuItem).ToList();
            this.CurrentControls.AddRange(menuItems);
        }

        private MenuItem BuildMenuItem(ButtonConfig button)
        {
            var header = new Grid();
            if (button.Icon != null)
            {
                header.Children.Add(button.Icon);
            }

            if (button.Text != null)
            {
                header.Children.Add(new TextBlock {Text = button.Text, FontSize = 12});
            }

            var menuItem = new MenuItem
            {
                Header = header,
                Command = button.Command,
                ToolTip = button.Tooltip,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            return menuItem;
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
        [Reactive] public bool MenuVisibility { get; set; }

        public ObservableRangeCollection<MenuItem> CurrentControls { get; set; } =
            new WpfObservableRangeCollection<MenuItem>();

        private void SelectDatabase_Click()
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();

            if (result != true)
                return;

            LocalDbContext.Reconnect(dialog.FileName);

            Settings.Default.DatabasePath = dialog.FileName;
            Settings.Default.Save();
        }

        private void OpenSchedulePage()
        {
            _host.AddPageAsync(new ScheduleToken("Schedule"));
        }

        private void Students_Click()
        {
            _host.AddPageAsync(new StudentTableToken("Студенты"));
        }

        private void Groups_Click()
        {
            _host.AddPageAsync(new GroupTableToken("Группы"));
        }

        private void SelectPhotoDir_Click()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                var result = dlg.ShowDialog();

                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPath))
                    return;
                Settings.Default.PhotoDirPath = dlg.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void AddStudent_Click()
        {
            _windowPageHost.AddPageAsync(new StudentFormToken("Добавить студента", new StudentEntity()));
        }

        private void AddLesson_Click()
        {
            _windowPageHost.AddPageAsync(new LessonFormToken("Добавить занятие", new LessonEntity(), _host));
        }

        private void AddGroup_Click()
        {
            _windowPageHost.AddPageAsync(new GroupFormToken("Добавить группу", new GroupEntity()));
        }

        private void AddStreamHandler()
        {
            _windowPageHost.AddPageAsync(new StreamFormToken("Добавить поток", new StreamEntity()));
        }

        private void OpenSettingsClick()
        {
            _host.AddPageAsync(new SettingsToken("Settings"));
        }

        protected override string GetLocalizationKey()
        {
            return "main";
        }
    }
}

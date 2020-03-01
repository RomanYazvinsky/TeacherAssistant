using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Containers;
using DynamicData;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.Tabs;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.DepartmentForm;
using TeacherAssistant.Forms.DisciplineForm;
using TeacherAssistant.Forms.GroupForm;
using TeacherAssistant.Forms.StreamForm;
using TeacherAssistant.Models;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.PageBase;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Pages.DepartmentTablePage;
using TeacherAssistant.Pages.DisciplineTablePage;
using TeacherAssistant.Pages.GroupTablePage;
using TeacherAssistant.Pages.LessonForm;
using TeacherAssistant.Pages.SettingsPage;
using TeacherAssistant.Properties;
using TeacherAssistant.Reader;
using TeacherAssistant.Services;
using TeacherAssistant.State;
using TeacherAssistant.StreamTable;
using TeacherAssistant.StudentForm;
using TeacherAssistant.StudentTable;
using TeacherAssistant.Utils;
using App = System.Windows.Application;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TeacherAssistant.Pages {
    public class PageControllerModel : AbstractModel<PageControllerModel> {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ModuleActivator _activator;
        private readonly WindowPageHost _windowPageHost;
        private readonly DatabaseManager _databaseManager;
        private readonly DatabaseBackupService _databaseBackupService;
        private readonly SerialUtil _serialUtil;
        private TabPageHost _host;

        public PageControllerModel(
            PageControllerToken token,
            ModuleActivator activator,
            PageControllerReducer reducer,
            SerialUtil serialUtil,
            MainReducer mainReducer,
            WindowPageHost windowPageHost,
            DatabaseManager databaseManager,
            DatabaseBackupService databaseBackupService
        ) {
            _activator = activator;
            _windowPageHost = windowPageHost;
            _databaseManager = databaseManager;
            _databaseBackupService = databaseBackupService;
            this._serialUtil = serialUtil;
            InitHandlers();
            ActivateContent(token);

            this.WhenActivated((c) => {
                mainReducer.Select(state => state.FullscreenMode)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(isFullScreen => this.MenuVisibility = !isFullScreen)
                    .DisposeWith(c);
                reducer.Select(state => state.SelectedPage)
                    .Where(LambdaHelper.NotNull)
                    .WithLatestFrom(reducer.Select(state => state.Controls), LambdaHelper.ToTuple)
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(tuple => {
                        var (selectedPage, controlsDict) = tuple;
                        var controls = selectedPage == null
                            ? new List<ButtonConfig>()
                            : controlsDict.GetOrDefault(selectedPage.Id) ?? new List<ButtonConfig>();
                        SetActionButtons(controls);
                    })
                    .DisposeWith(c);
                _serialUtil.ConnectionStatus
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Select(status => status.IsConnected)
                    .Subscribe(status => {
                        this.ReaderMenuText = status
                            ? Localization["Отключить считыватель"]
                            : Localization["Включить считыватель"];
                    })
                    .DisposeWith(c);
            });
        }

        private void InitHandlers() {
            this.OpenScheduleHandler = new CommandHandler(OpenSchedulePage);
            this.OpenStudentsTableHandler = new CommandHandler(Students_Click);
            this.OpenGroupsTableHandler = new CommandHandler(Groups_Click);
            this.OpenStreamsTableHandler = new CommandHandler(OpenStreams);
            this.OpenDepartmentsTableHandler = new CommandHandler(OpenDepartments);
            this.OpenSelectPhotoDirectoryDialogHandler = new CommandHandler(SelectPhotoDir_Click);
            this.OpenSelectDatabaseDialogHandler = new CommandHandler(SelectDatabase_Click);
            this.OpenSettingsHandler = new CommandHandler(OpenSettingsClick);
            this.OpenAddStudentFormHandler = new CommandHandler(AddStudent_Click);
            this.OpenAddLessonFormHandler = new CommandHandler(AddLesson_Click);
            this.OpenAddGroupFormHandler = new CommandHandler(AddGroup_Click);
            this.OpenAddStreamFormHandler = new CommandHandler(AddStream);
            this.OpenAddDisciplineFormHandler = new CommandHandler(AddDiscipline);
            this.OpenAddDepartmentFormHandler = new CommandHandler(AddDepartment);
            this.ToggleCardReaderHandler = ReactiveCommand.Create(ToggleCardReader);
            this.OpenDisciplinesTableHandler = ReactiveCommand.Create(OpenDisciplines);
            this.CreateBackupHandler = ReactiveCommand.Create(() => _databaseBackupService.BackupDatabase());
        }

        private void SetActionButtons(List<ButtonConfig> buttons) {
            foreach (var buttonConfig in buttons.Where(config => config.Icon != null)) {
                var dependencyObject = VisualTreeHelper.GetParent(buttonConfig.Icon);
                ((Grid) dependencyObject)?.Children.Remove(buttonConfig.Icon);
            }

            this.CurrentControls.Clear();
            var items = new List<ButtonConfig>(buttons);
            items.Reverse();
            var menuItems = items.Select(BuildMenuItem).ToList();
            this.CurrentControls.AddRange(menuItems);
        }

        private MenuItem BuildMenuItem(ButtonConfig button) {
            var header = new Grid();
            if (button.Icon != null) {
                header.Children.Add(button.Icon);
            }

            if (button.Text != null) {
                header.Children.Add(new TextBlock {Text = button.Text, FontSize = 12});
            }

            var menuItem = new MenuItem {
                Header = header,
                Command = button.Command,
                ToolTip = button.Tooltip,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            return menuItem;
        }

        private void ActivateContent(PageControllerToken token) {
            var tabControllerToken = new TabControllerToken();
            if (token.Content == null) {
                _activator.ActivateAsync(tabControllerToken)
                    .ToObservable()
                    .ObserveOnDispatcher(DispatcherPriority.Background)
                    .Subscribe(module => {
                        _host = module.Injector.Locate<TabPageHost>();
                        this.CentralControl = module.GetEntryComponent();
                        _host.AddPageAsync(token.ContentToken);
                    });
            }
            else {
                var tabModule = _activator.Activate(tabControllerToken);
                _host = tabModule.Injector.Locate<TabPageHost>();
                this.CentralControl = tabModule.GetEntryComponent();
                // _host.Attach(token.Content);
            }
        }


        [Reactive] public Control CentralControl { get; set; }

        [Reactive] public string ReaderMenuText { get; set; }

        public ICommand OpenScheduleHandler { get; set; }
        public ICommand OpenStudentsTableHandler { get; set; }
        public ICommand OpenGroupsTableHandler { get; set; }
        public ICommand OpenStreamsTableHandler { get; set; }
        public ICommand OpenDisciplinesTableHandler { get; set; }
        public ICommand OpenDepartmentsTableHandler { get; set; }
        public ICommand OpenSelectPhotoDirectoryDialogHandler { get; set; }
        public ICommand OpenSelectDatabaseDialogHandler { get; set; }
        public ICommand ToggleCardReaderHandler { get; set; }
        public ICommand CreateBackupHandler { get; set; }
        public ICommand OpenSettingsHandler { get; set; }
        public ICommand OpenAddStudentFormHandler { get; set; }
        public ICommand OpenAddLessonFormHandler { get; set; }
        public ICommand OpenAddStreamFormHandler { get; set; }
        public ICommand OpenAddDisciplineFormHandler { get; set; }
        public ICommand OpenAddDepartmentFormHandler { get; set; }
        public ICommand OpenAddGroupFormHandler { get; set; }
        [Reactive] public bool MenuVisibility { get; set; }

        public ObservableCollection<MenuItem> CurrentControls { get; set; } =
            new ObservableCollection<MenuItem>();

        private async void SelectDatabase_Click() {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();

            if (result != true)
                return;

            try {
                await _databaseManager.Connect(dialog.FileName);
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Error, "Cannot connect selected database");
                Logger.Log(LogLevel.Error, e);
                return;
            }
            Settings.Default.DatabasePath = dialog.FileName;
            Settings.Default.Save();
            foreach (var page in _host.Pages.ToList()) {
                _host.ClosePage(page.Key);
            }
        }

        private void ToggleCardReader() {
            if (_serialUtil.IsRunning) {
                _serialUtil.Close();
            }
            else {
                _serialUtil.Start();
            }
        }

        private void OpenSchedulePage() {
            _host.AddPageAsync(new ScheduleToken("Расписание"));
        }

        private void Students_Click() {
            _host.AddPageAsync(new StudentTableToken("Студенты"));
        }

        private void Groups_Click() {
            _host.AddPageAsync(new GroupTableToken("Группы"));
        }

        private void OpenStreams() {
            _host.AddPageAsync(new StreamTableToken("Потоки"));
        }
        private void OpenDisciplines() {
            _host.AddPageAsync(new DisciplineTableToken("Дисциплины"));
        }
        private void OpenDepartments() {
            _host.AddPageAsync(new DepartmentTableToken("Дисциплины"));
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
            _windowPageHost.AddPageAsync(new StudentFormToken("Добавить студента", new StudentEntity()));
        }

        private void AddLesson_Click() {
            _windowPageHost.AddPageAsync(new LessonFormToken("Добавить занятие", new LessonEntity(), _host));
        }

        private void AddGroup_Click() {
            _windowPageHost.AddPageAsync(new GroupFormToken("Добавить группу", new GroupEntity()));
        }

        private void AddStream() {
            _windowPageHost.AddPageAsync(new StreamFormToken("Добавить поток", new StreamEntity()));
        }
        private void AddDiscipline() {
            _windowPageHost.AddPageAsync(new DisciplineFormToken("Дисциплина"));
        }
        private void AddDepartment() {
            _windowPageHost.AddPageAsync(new DepartmentFormToken("Дисциплина"));
        }

        private void OpenSettingsClick() {
            _host.AddPageAsync(new SettingsToken("Настройки"));
        }

        protected override string GetLocalizationKey() {
            return "main";
        }
    }
}

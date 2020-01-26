using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using Containers;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.Tabs;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ComponentsImpl.SchedulePage;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Dao;
using TeacherAssistant.Properties;
using TeacherAssistant.ReaderPlugin;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TeacherAssistant.Pages {
    public class PageControllerModel : AbstractModel {
        private SerialUtil SerialUtil { get; }

        public PageControllerModel(ModuleLoader loader, PageControllerReducer reducer, SerialUtil serialUtil) {
            this.SerialUtil = serialUtil;
            InitHandlers();
            var tabControllerToken = new TabControllerToken(new ScheduleToken("Schedule"));
            var tabControllerModule = loader.Activate(tabControllerToken);
            this.CentralControl = tabControllerModule.GetEntryComponent();
            reducer.Select(state => state.Controls)
                .Where(NotNull)
                .Subscribe(controls => {
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
            // this._pageService.OpenPage
            // (
            //     this.Id,
            //     PageConfigs.SchedulePageConfig
            // );
        }

        private void Students_Click() {
            // this._pageService.OpenPage
            // (
            //     this.Id,
            //     new PageProperties<StudentTable.StudentTable> {
            //         Header = "Студенты",
            //         MinHeight = 600,
            //         MinWidth = 600,
            //     }
            // );
        }

        private void Groups_Click() {
            // this._pageService.OpenPage
            // (
            //     this.Id,
            //     new PageProperties<GroupTable.GroupTable> {
            //         Header = "Группы",
            //         MinHeight = 600,
            //         MinWidth = 600,
            //     }
            // );
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
            // var pageId = this._pageService.OpenPage
            // (
            //     "Modal",
            //     new PageProperties<StudentForm.StudentForm> {
            //         Header = "Добавить студента",
            //         MinHeight = 700,
            //         MinWidth = 800,
            //     }
            // );
            // StoreManager.Publish(pageId + ".Student", new StudentEntity());
        }

        private void AddLesson_Click() {
            // var pageId = this._pageService.OpenPage
            // (
            //     "Modal",
            //     new PageProperties<LessonForm.LessonForm> {
            //         Header = "Добавить занятие",
            //         MinHeight = 550,
            //         MinWidth = 650,
            //     }
            // );
            // StoreManager.Publish(pageId + ".LessonChange", new LessonEntity());
        }

        private void AddGroup_Click() {
            // var pageId = this._pageService.OpenPage
            // (
            //     "Modal",
            //     new PageProperties<GroupForm> {
            //         Header = "Добавить группу",
            //         MinHeight = 500,
            //         MinWidth = 650,
            //     }
            // );
            // StoreManager.Publish(new GroupEntity(), pageId, "GroupChange");
        }

        private void AddStreamHandler() {
            // var pageId = this._pageService.OpenPage
            // (
            //     "Modal",
            //     new PageProperties<StreamForm> {
            //         Header = "Добавить поток",
            //         MinHeight = 500,
            //         MinWidth = 650,
            //     }
            // );
            // StoreManager.Publish(new StreamEntity(), pageId, "StreamChange");
        }

        private void OpenSettingsClick() {
            // this._pageService.OpenPage
            // (
            //     this.Id, 
            //     new PageProperties<SettingsPage.SettingsPage> {
            //         Header = "Настройки",
            //         MinHeight = 500,
            //         MinWidth = 650,
            //     }
            // );
        }


        public void ManageHotKeys(KeyEventArgs e) {
            // if (e.Key == Key.F11) {
            //     new Storage.ToggleFullscreen().Dispatch();
            // }
        }

        protected override string GetLocalizationKey() {
            return "main";
        }
    }
}
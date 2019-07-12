using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
using TeacherAssistant.Dao;
using TeacherAssistant.Footer.TaskExpandList;
using TeacherAssistant.Forms.GroupForm;
using TeacherAssistant.Forms.StreamForm;
using TeacherAssistant.Properties;
using TeacherAssistant.ReaderPlugin;
using TeacherAssistant.State;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TeacherAssistant.Pages {
    public class MainWindowPageModel : AbstractModel {
        private ISerialUtil SerialUtil { get; }

        public MainWindowPageModel(string id, ISerialUtil serialUtil) : base(id) {
            this.SerialUtil = serialUtil;
            this.OpenSchedule = new CommandHandler(OpenSchedulePage);
            this.OpenStudentsTable = new CommandHandler(Students_Click);
            this.OpenGroupsTable = new CommandHandler(Groups_Click);
            this.OpenSelectPhotoDirectoryDialog = new CommandHandler(SelectPhotoDir_Click);
            this.OpenSelectDatabaseDialog = new CommandHandler(SelectDatabase_Click);
            this.OpenAddStudentForm = new CommandHandler(AddStudent_Click);
            this.OpenAddLessonForm = new CommandHandler(AddLesson_Click);
            this.OpenAddGroupForm = new CommandHandler(AddGroup_Click);
            this.OpenAddStreamForm = new CommandHandler(AddStreamHandler);
            var tabManager = Injector.Get<TabManager>(("id", id));
            this.CentralControl = tabManager;
            Select<List<ButtonConfig>>(this.Id, "Controls").Where(NotNull).Subscribe(controls => {
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

        [Reactive] public Control CentralControl { get; set; }

        public CommandHandler OpenSchedule { get; set; }
        public CommandHandler OpenStudentsTable { get; set; }
        public CommandHandler OpenGroupsTable { get; set; }
        public CommandHandler OpenSelectPhotoDirectoryDialog { get; set; }
        public CommandHandler OpenSelectDatabaseDialog { get; set; }
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


            var taskHandler = new TaskHandler
            (
                "Загрузка БД",
                false,
                actions => {
                    void OnGeneralDbContextOnDatabaseChanged(object o, string a) {
                        GeneralDbContext.DatabaseChanged -= OnGeneralDbContextOnDatabaseChanged;
                        actions.Complete();
                    }

                    GeneralDbContext.DatabaseChanged += OnGeneralDbContextOnDatabaseChanged;
                }
            );

            StoreManager.Add("TaskList", taskHandler);
            taskHandler.Start();
            GeneralDbContext.Reconnect(dialog.FileName);

            Settings.Default.DatabasePath = dialog.FileName;
            Settings.Default.Save();
        }

        private void OpenSchedulePage() {
            this.PageService.OpenPage
            (
                this.Id,
                PageConfigs.SchedulePageConfig
            );
        }

        private void Students_Click() {
            this.PageService.OpenPage
            (
                this.Id,
                new PageProperties {
                    Header = "Студенты",
                    MinHeight = 600,
                    MinWidth = 600,
                    PageType = typeof(StudentTable.StudentTable)
                }
            );
        }

        private void Groups_Click() {
            this.PageService.OpenPage
            (
                this.Id,
                new PageProperties {
                    Header = "Группы",
                    MinHeight = 600,
                    MinWidth = 600,
                    PageType = typeof(GroupTable.GroupTable)
                }
            );
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
            var pageId = this.PageService.OpenPage
            (
                "Modal",
                new PageProperties {
                    Header = "Добавить студента",
                    MinHeight = 700,
                    MinWidth = 800,
                    PageType = typeof(StudentForm.StudentForm)
                }
            );
            StoreManager.Publish(pageId + ".Student", new StudentModel());
        }

        private void AddLesson_Click() {
            var pageId = this.PageService.OpenPage
            (
                "Modal",
                new PageProperties {
                    Header = "Добавить занятие",
                    MinHeight = 550,
                    MinWidth = 650,
                    PageType = typeof(LessonForm.LessonForm)
                }
            );
            StoreManager.Publish(pageId + ".LessonChange", new LessonModel());
        }

        private void AddGroup_Click() {
            var pageId = this.PageService.OpenPage
            (
                "Modal",
                new PageProperties {
                    Header = "Добавить группу",
                    MinHeight = 500,
                    MinWidth = 650,
                    PageType = typeof(GroupForm),
                }
            );
            StoreManager.Publish(new GroupModel(), pageId, "GroupChange");
        }

        private void AddStreamHandler() {
            var pageId = this.PageService.OpenPage
            (
                "Modal",
                new PageProperties {
                    Header = "Добавить поток",
                    MinHeight = 500,
                    MinWidth = 650,
                    PageType = typeof(StreamForm),
                }
            );
            StoreManager.Publish(new StreamModel(), pageId, "StreamChange");
        }


        public void ManageHotKeys(KeyEventArgs e) {
            if (e.Key == Key.F11) {
                new Storage.ToggleFullscreen().Dispatch();
            }
        }

        protected override string GetLocalizationKey() {
            return "main";
        }

        public override Task Init() {
            return Task.CompletedTask;
        }
    }
}
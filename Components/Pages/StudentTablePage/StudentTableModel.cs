using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Footer.TaskExpandList;
using TeacherAssistant.State;

namespace TeacherAssistant.StudentTable {
    public partial class StudentTableModel : AbstractModel {
        private static readonly string LocalizationKey = "page.student.table";

        public StudentTableModel(string id,
            IPhotoService photoService
        ) : base(id) {
            this.PhotoService = photoService;
            this.WhenAnyValue
                    (model => model.SelectedStudentModel)
                .Where(NotNull)
                .Subscribe(model => UpdatePhoto(((StudentViewModel) model).Model));
            this.ShowStudent = new CommandHandler
            (
                () => {
                    var pageId = PageService.OpenPage
                    (new PageProperties {
                            PageType = typeof(StudentViewPage.StudentViewPage),
                            Header = ((StudentViewModel) this.SelectedStudentModel).Model.LastName
                        },
                        this.Id);
                    StoreManager.Publish(((StudentViewModel) this.SelectedStudentModel).Model, pageId, "Student");
                }
            );
            this.DeleteStudent = new CommandHandler
            (
                () => {
                    var student = _db.StudentModels.Find(((StudentViewModel) this.SelectedStudentModel).Model.Id);
                    var studentLessonModels = _db.StudentLessonModels.Where(model => model._StudentId == student.Id);
                    _db.StudentModels.Remove(student);
                    _db.SaveChangesAsync();
                    this.Students.Remove(this.SelectedStudentModel);
                }
            );
        }

        public override List<ButtonConfig> GetControls() {
            var buttonConfigs = base.GetControls();
            buttonConfigs.Add(new ButtonConfig {
                Command = new CommandHandler
                (
                    () => {
                        var taskHandler = new TaskHandler
                        (
                            "Загрузка фото",
                            this.Students.Count,
                            true,
                            async actions => {
                                var items = this.Students.Select(model => ((StudentViewModel) model).Model).ToList();
                                foreach (var studentModel in items) {
                                    if (actions.IsCancelled()) {
                                        actions.ConfirmCancel();
                                        break;
                                    }

                                    await this.PhotoService.DownloadPhoto
                                            (StudentModel.CardUidToId(studentModel.CardUid))
                                        .ConfigureAwait(false);
                                    actions.Next();
                                }

                                actions.Complete();
                            }
                        );
                        StoreManager.Add("TaskList", taskHandler);
                        taskHandler.Start();
                    }
                ),
                Text = "Загрузить фото"
            });
            return buttonConfigs;
        }

        private IPhotoService PhotoService { get; }

        public ObservableRangeCollection<object> Students { get; set; } =
            new WpfObservableRangeCollection<object>();

        [Reactive] public object SelectedStudentModel { get; set; }
        public CommandHandler ShowStudent { get; set; }
        public CommandHandler DeleteStudent { get; set; }

        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection> {
            {"Model.LastName", ListSortDirection.Ascending},
            {"Model.FirstName", ListSortDirection.Ascending}
        };

        [Reactive] public BitmapImage StudentPhoto { get; set; }

        public Func<object, string, bool> FilterFunction { get; set; } = (o, s) => {
            s = s.ToUpperInvariant();
            var studentViewModel = (StudentViewModel) o;
            var student = studentViewModel.Model;
            return student.FirstName != null && student.FirstName.ToUpperInvariant().Contains(s)
                   || student.LastName != null && student.LastName.ToUpperInvariant().Contains(s)
                   || student.SecondName != null && student.SecondName.ToUpperInvariant().Contains(s)
                   || studentViewModel.GroupsText.ToUpperInvariant().Contains(s);
        };

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public override Task Init() {
            this.Students.Clear();

            Select<object>("").ObserveOn(RxApp.MainThreadScheduler).Subscribe(async _ => {
                this.Students.Clear();
                (await _db.StudentModels.Include("Groups")
                    .ToListAsync()).ForEach(model => this.Students.Add(new StudentViewModel(model)));
            });


            return Task.CompletedTask;
        }


        private void UpdatePhoto(StudentModel model) {
            if (model == null) {
                return;
            }

            this.StudentPhoto = null;
            Task.Run
                (
                    async () => {
                        var photoPath = await this.PhotoService.DownloadPhoto
                                (StudentModel.CardUidToId(model.CardUid))
                            .ConfigureAwait(false);
                        if (photoPath == null) {
                            return;
                        }

                        var image = this.PhotoService.GetImage(photoPath);
                        UpdateFromAsync(() => this.StudentPhoto = image);
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
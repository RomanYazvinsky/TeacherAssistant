using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Pages.StudentTablePage.ViewModels;

namespace TeacherAssistant.StudentTable {
    public class StudentTableModel : AbstractModel {
        private static readonly string LocalizationKey = "page.student.table";

        public StudentTableModel(PhotoService photoService) {
            this.PhotoService = photoService;
            this.StudentTableConfig = new TableConfig {
                Sorts = this.Sorts,
                Filter = this.FilterFunction,
                DragConfig = new DragConfig {
                    DragValuePath = nameof(StudentViewModel.Student)
                }
            };
            this.StudentTableConfig.SelectedItem
                .AsObservable()
                .Where(NotNull)
                .Subscribe(model => UpdatePhoto(((StudentViewModel) model).Student));
            this.ShowStudent = new CommandHandler
            (
                () => {
                    var selectedItem = this.StudentTableConfig.SelectedItem.Value;
                    if (selectedItem == null) {
                        return;
                    }

                    // var pageId = this._pageService.OpenPage
                    // (new PageProperties<StudentViewPage.StudentViewPage> {
                    //         Header = ((StudentViewModel) selectedItem).Student.LastName
                    //     },
                    //     this.Id);
                    // StoreManager.Publish(((StudentViewModel) selectedItem).Student, pageId, "Student");
                }
            );
            this.DeleteStudent = new CommandHandler
            (
                () => {
                    var selectedItem = this.StudentTableConfig.SelectedItem.Value;
                    if (selectedItem == null) {
                        return;
                    }

                    // Database.Students.Remove(((StudentViewModel) selectedItem).Student);
                    // Database.SaveChangesAsync();
                    this.StudentTableConfig.TableItems.Remove(selectedItem);
                }
            );
            this.RefreshSubject.AsObservable().ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => {
                this.StudentTableConfig.TableItems.Clear();
                // var studentEntities = Database.Students
                //     .Include("Groups")
                //     .ToList();
                // var studentViewModels = studentEntities.Select(entity => new StudentViewModel(entity)).ToList();
                // this.StudentTableConfig.TableItems.AddRange(studentViewModels);
            });
        }

        public override List<ButtonConfig> GetControls() {
            var buttonConfigs = base.GetControls();
            buttonConfigs.Add(new ButtonConfig {
                Command = new CommandHandler
                (
                    () => {
                        // var taskHandler = new TaskHandler
                        // (
                        //     "Загрузка фото",
                        //     this.StudentTableConfig.TableItems.Count,
                        //     true,
                        //     async progressControl => {
                        //         var items = this.StudentTableConfig.TableItems
                        //             .Select(model => ((StudentViewModel) model).Student).ToList();
                        //         for (var i = 0; i < items.Count; i += 5) {
                        //             if (progressControl.IsCancelled()) {
                        //                 progressControl.ConfirmCancel();
                        //                 break;
                        //             }
                        //
                        //             var portion = items.GetRange(i, 5);
                        //             await LoadImages(portion);
                        //             progressControl.Next();
                        //         }
                        //
                        //         progressControl.Complete();
                        //     }
                        // );
                        // StoreManager.Add("TaskList", taskHandler);
                        // taskHandler.Start();
                    }
                ),
                Text = "Загрузить фото"
            });
            return buttonConfigs;
        }

        private Task<string[]> LoadImages(IEnumerable<StudentEntity> students) {
            return Task.WhenAll(students.Select(entity =>
                this.PhotoService.DownloadPhoto(StudentEntity.CardUidToId(entity.CardUid))));
        }

        private PhotoService PhotoService { get; }


        public TableConfig StudentTableConfig { get; set; }
        public CommandHandler ShowStudent { get; set; }
        public CommandHandler DeleteStudent { get; set; }

        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection> {
            {"Student.LastName", ListSortDirection.Ascending},
            {"Student.FirstName", ListSortDirection.Ascending}
        };

        [Reactive] public BitmapImage StudentPhoto { get; set; }

        public Func<object, string, bool> FilterFunction { get; set; } = (o, s) => {
            s = s.ToUpperInvariant();
            var studentViewModel = (StudentViewModel) o;
            var student = studentViewModel.Student;
            return student.FirstName != null && student.FirstName.ToUpperInvariant().Contains(s)
                   || student.LastName != null && student.LastName.ToUpperInvariant().Contains(s)
                   || student.SecondName != null && student.SecondName.ToUpperInvariant().Contains(s)
                   || studentViewModel.GroupsText.ToUpperInvariant().Contains(s);
        };

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        private void UpdatePhoto(StudentEntity entity) {
            if (entity == null) {
                return;
            }

            this.StudentPhoto = null;
            Task.Run
                (
                    async () => {
                        var photoPath = await this.PhotoService.DownloadPhoto
                                (StudentEntity.CardUidToId(entity.CardUid))
                            .ConfigureAwait(false);
                        if (photoPath == null) {
                            return;
                        }

                        var image = this.PhotoService.GetImage(photoPath);
                        RunInUiThread(() => this.StudentPhoto = image);
                    }
                )
                .ConfigureAwait(false);
        }
    }
}
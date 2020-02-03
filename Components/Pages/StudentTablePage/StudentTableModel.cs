using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Containers;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Pages;
using TeacherAssistant.Pages.StudentTablePage.ViewModels;
using TeacherAssistant.StudentViewPage;

namespace TeacherAssistant.StudentTable {
    public class StudentTableModel : AbstractModel {
        private static readonly string LocalizationKey = "page.student.table";

        public StudentTableModel(StudentTableToken token,
            PageControllerReducer reducer,
            PhotoService photoService,
            TabPageHost pageHost,
            LocalDbContext context) {
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

                    var studentEntity = ((StudentViewModel) selectedItem).Student;
                    pageHost.AddPage(new StudentViewPageToken(studentEntity.LastName, studentEntity));
                }
            );
            this.DeleteStudent = new CommandHandler
            (
                () => {
                    var selectedItem = this.StudentTableConfig.SelectedItem.Value;
                    if (selectedItem == null) {
                        return;
                    }

                    context.Students.Remove(((StudentViewModel) selectedItem).Student);
                    context.SaveChangesAsync();
                    this.StudentTableConfig.TableItems.Remove(selectedItem);
                }
            );
            this.RefreshSubject
                .AsObservable()
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe(_ => {
                    this.StudentTableConfig.TableItems.Clear();
                    var studentEntities = context.Students
                        .Include("Groups")
                        .ToList();
                    var studentViewModels = studentEntities.Select(entity => new StudentViewModel(entity)).ToList();
                    this.StudentTableConfig.TableItems.AddRange(studentViewModels);
                });
            reducer.Dispatch(new RegisterControlsAction(token, GetControls()));
        }

        public List<ButtonConfig> GetControls() {
            var buttonConfigs = new List<ButtonConfig> {
                GetRefreshButtonConfig(),
                new ButtonConfig {
                    Command = new CommandHandler(() => {
                        // todo load images
                        // var items = this.Students.Select(model => ((StudentViewModel) model).Model).ToList();
                        // foreach (var studentModel in items) {
                        //    await this.PhotoService.DownloadPhoto
                        //             (StudentModel.CardUidToId(studentModel.CardUid))
                        //         .ConfigureAwait(false);
                        // }
                    }),
                    Text = "Загрузить фото"
                }
            };
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
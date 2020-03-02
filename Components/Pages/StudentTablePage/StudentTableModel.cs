using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Containers;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages;
using TeacherAssistant.Pages.PageController;
using TeacherAssistant.Pages.StudentTablePage.ViewModels;
using TeacherAssistant.Services;
using TeacherAssistant.StudentViewPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.StudentTable {
    public class StudentTableModel : AbstractModel<StudentTableModel> {
        private readonly LocalDbContext _context;
        private static readonly string LocalizationKey = "page.student.table";

        public StudentTableModel(
            StudentTableToken token,
            PageControllerReducer reducer,
            PhotoService photoService,
            TabPageHost pageHost,
            LocalDbContext context) {
            _context = context;
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
                .Where(LambdaHelper.NotNull)
                .Subscribe(model => UpdatePhoto(((StudentViewModel) model).Student));
            this.ShowStudent = new CommandHandler(() => {
                    var selectedItem = this.StudentTableConfig.SelectedItem.Value;
                    if (selectedItem == null) {
                        return;
                    }

                    var studentEntity = ((StudentViewModel) selectedItem).Student;
                    pageHost.AddPageAsync(new StudentViewPageToken(studentEntity.LastName, studentEntity));
                }
            );
            this.DeleteStudent = ReactiveCommand.Create
            (
                () => {
                    var selectedItem = this.StudentTableConfig.SelectedItem.Value;
                    if (selectedItem == null) {
                        return;
                    }

                    var student = ((StudentViewModel) selectedItem).Student;
                    var persistentEntity = context.Students.Find(student.Id);
                    if (persistentEntity != null) {
                        context.Students.Remove(persistentEntity);
                        context.SaveChanges();
                    }
                    RunInUiThread(() => {
                        this.StudentTableConfig.TableItems.Remove(selectedItem);
                    });
                }
            );
            Init();
            reducer.Dispatch(new RegisterControlsAction(token, GetControls()));
        }

        private void Init() {
            this.StudentTableConfig.TableItems.Clear();
            var studentEntities = _context.Students
                .Include(student => student.Groups)
                .ToList();
            var studentViewModels = studentEntities.Select(entity => new StudentViewModel(entity)).ToList();
            this.StudentTableConfig.TableItems.AddRange(studentViewModels);
        }

        public List<ButtonConfig> GetControls() {
            var buttonConfigs = new List<ButtonConfig> {
                new ButtonConfig {
                    Command = ReactiveCommand.Create(async () => {
                        var items = await _context.Students.ToListAsync();
                        await LoadImages(items);
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
        public ICommand ShowStudent { get; set; }
        public ICommand DeleteStudent { get; set; }

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
            Task.Run(async () => {
                    var photoPath = await this.PhotoService.DownloadPhoto(StudentEntity.CardUidToId(entity.CardUid));
                    if (photoPath == null) {
                        return;
                    }

                    var image = this.PhotoService.GetImage(photoPath);
                    RunInUiThread(() => this.StudentPhoto = image);
                }
            );
        }
    }
}
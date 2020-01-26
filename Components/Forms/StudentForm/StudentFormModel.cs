using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.StudentForm {
    public class StudentFormModel : AbstractModel, IValidatableViewModel {
        public static readonly string LocalizationKey = "student.form";
        private PhotoService _photoService;
        private StudentEntity _originalStudent;

        public StudentFormModel(
            string id,
            PhotoService photoService,
            StudentCardService studentCardService
        ) {
            _photoService = photoService;
            this.ChosenGroupTableConfig = new TableConfig {
                Sorts = this.ChosenGroupSorts
            };
            this.AvailableGroupTableConfig = new TableConfig {
                Sorts = this.AvailableGroupSorts
            };
            this.SelectGroups = new ButtonConfig {
                Text = "->",
                Command = new CommandHandler
                (
                    () => {
                        foreach (var selectedAvailableGroup in this.SelectedAvailableGroups.Cast<GroupEntity>().ToList()
                        ) {
                            this.AvailableGroups.Remove(selectedAvailableGroup);
                            var choseGroupModel = new ChoseGroupModel(selectedAvailableGroup);
                            this.ChoseGroups.Add(choseGroupModel);
                        }
                    }
                )
            };
            this.DeselectGroups = new ButtonConfig {
                Text = "<-",
                Command = new CommandHandler
                (
                    () => {
                        foreach (var selectedChoseGroup in this.SelectedChoseGroups.Cast<ChoseGroupModel>().ToList()) {
                            this.ChoseGroups.Remove(selectedChoseGroup);
                            this.AvailableGroups.Add(selectedChoseGroup.Group);
                        }
                    }
                )
            };
            this.SwitchViewButtonConfig = new ButtonConfig {
                Command = new CommandHandler
                    (() => { this.IsShowPhoto = !this.IsShowPhoto; })
            };
            this.SaveButtonConfig = new ButtonConfig {
                Command = new CommandHandler
                (
                    () => {
                        var isNew = this.Student.Id == 0;
                        this.Student.Groups =
                            this.ChoseGroups.Cast<ChoseGroupModel>().Select(model => model.Group).ToList();
                        _originalStudent.Apply(this.Student);
                        if (isNew) {
                            LocalDbContext.Instance.Students.Add(_originalStudent);
                        }

                        foreach (ChoseGroupModel selectedChoseGroup in this.ChoseGroups) {
                            if (selectedChoseGroup.IsPraepostor) {
                                selectedChoseGroup.Group.Chief = _originalStudent;
                            }
                            else {
                                if (selectedChoseGroup.Group._PraepostorId == _originalStudent.Id) {
                                    selectedChoseGroup.Group.Chief = null;
                                }
                            }
                        }

                        LocalDbContext.Instance.SaveChangesAsync();
                        // this._pageService.ClosePage(this.Id);
                    }
                )
            };
            this.WhenAnyValue(model => model.IsShowPhoto)
                .CombineLatest
                (
                    this.WhenAnyValue(model => model.Student, model => model?.CardUid)
                        .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 6),
                    (b, s) => new Tuple<bool, string>(b, s)
                )
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe
                (
                    async tuple => {
                        var (isStudentCardSelected, cardUid) = tuple;
                        if (string.IsNullOrEmpty(cardUid)) {
                            RunInUiThread(() => this.StudentPhoto = null);
                            return;
                        }

                        if (!isStudentCardSelected)
                            return;

                        var path = await _photoService.DownloadPhoto(StudentEntity.CardUidToId(cardUid));
                        if (string.IsNullOrEmpty(path))
                            return;
                        var photo = _photoService.GetImage(path);
                        RunInUiThread(() => { this.StudentPhoto = photo; });
                    }
                );
            this.WhenAnyValue(model => model.SelectedStudentCard)
                .Subscribe
                (
                    card => {
                        if (card == null) {
                            return;
                        }

                        this.Student.CardUid = card.CardUid;
                        this.Student.FirstName = card.FirstName;
                        this.Student.LastName = card.LastName;
                        this.Student.SecondName = card.SecondName;
                    }
                );

            ManageObservable(studentCardService.ReadStudentCards.Changes())
                .Subscribe(onNext: _ => this.ReadStudents = new List<StudentCard>(studentCardService.ReadStudentCards));
            // Select<StudentEntity>(this.Id, "Student").Subscribe(Initialize);
        }

        private void Initialize(StudentEntity entity) {
            if (entity == null)
                return;
            _originalStudent = entity;
            this.Student = new StudentEntity(entity);
            this.ChoseGroups.Clear();
            var choseGroupModels = entity.Groups.Select(group => new ChoseGroupModel(group) {
                IsPraepostor = group.Chief != null && group.Chief.Id == entity.Id,
                IsPraepostorAlreadySet = group.Chief != null && @group.Chief.Id > 0
            }).ToList();
            this.ChoseGroups.AddRange(choseGroupModels);
            var groupModels = LocalDbContext.Instance.Groups.Include(group => group.Students)
                .AsEnumerable()
                .Where
                (
                    groupModel => groupModel._IsActive == 1
                                  && entity.Groups.All(studentGroup => studentGroup.Id != groupModel.Id)
                )
                .ToList();
            this.AvailableGroups.Clear();
            this.AvailableGroups.AddRange(groupModels);
            this.IsShowPhoto = entity.Id != 0;
        }
        
        public TableConfig ChosenGroupTableConfig { get; set; }
        public TableConfig AvailableGroupTableConfig { get; set; }

        private ObservableRangeCollection<object> AvailableGroups => this.AvailableGroupTableConfig.TableItems;

        private ObservableRangeCollection<object> ChoseGroups => this.ChosenGroupTableConfig.TableItems;

        private IEnumerable<object> SelectedAvailableGroups => this.AvailableGroupTableConfig.SelectedItems;

        private IEnumerable<object> SelectedChoseGroups => this.ChosenGroupTableConfig.SelectedItems;

        public ButtonConfig SelectGroups { get; set; }
        public ButtonConfig DeselectGroups { get; set; }

        public ButtonConfig SwitchViewButtonConfig { get; set; }
        public ButtonConfig SaveButtonConfig { get; set; }
        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public StudentEntity Student { get; set; }
        [Reactive] public List<StudentCard> ReadStudents { get; set; } = new List<StudentCard>();

        public Dictionary<string, ListSortDirection> AvailableGroupSorts { get; set; } = new Dictionary<string, ListSortDirection> {
            {"Name", ListSortDirection.Ascending}
        };

        public Dictionary<string, ListSortDirection> ChosenGroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {"Group.Name", ListSortDirection.Ascending}
            };

        [Reactive] public StudentCard SelectedStudentCard { get; set; }

        [Reactive] public bool IsShowPhoto { get; set; }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public class ChoseGroupModel {
            public ChoseGroupModel(GroupEntity group) {
                this.Group = group;
            }

            public GroupEntity Group { get; }
            public bool IsPraepostor { get; set; }

            public bool IsPraepostorAlreadySet { get; set; }
        }

        public ValidationContext ValidationContext { get; } = new ValidationContext();
    }
}
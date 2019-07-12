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
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.ReaderPlugin;

namespace TeacherAssistant.StudentForm {
    public class StudentFormModel : AbstractModel, IValidatableViewModel {
        public static readonly string LocalizationKey = "student.form";
        private IPhotoService _photoService;
        private StudentModel _originalStudent;

        public StudentFormModel(
            string id,
            IPhotoService photoService,
            StudentCardService studentCardService
        ) : base(id) {
            _photoService = photoService;
            this.ValidationRule
            (
                model => model.Student.LastName,
                s => !string.IsNullOrWhiteSpace(s),
                Localization["student.form.validation.wrong.name"]
            );
            this.SelectGroups = new ButtonConfig {
                Text = "->",
                Command = new CommandHandler
                (
                    () => {
                        foreach (var selectedAvailableGroup in this.SelectedAvailableGroups.Cast<GroupModel>().ToList()
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
                            _db.StudentModels.Add(_originalStudent);
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

                        _db.SaveChangesAsync();
                        this.PageService.ClosePage(this.Id);
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
                            UpdateFromAsync(() => this.StudentPhoto = null);
                            return;
                        }

                        if (!isStudentCardSelected)
                            return;

                        var path = await _photoService.DownloadPhoto(StudentModel.CardUidToId(cardUid));
                        if (string.IsNullOrEmpty(path))
                            return;
                        var photo = _photoService.GetImage(path);
                        UpdateFromAsync(() => { this.StudentPhoto = photo; });
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

            ManageObservable
                (
                    Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>
                    (
                        (handler) => studentCardService.ReadStudentCards.CollectionChanged += handler,
                        handler => studentCardService.ReadStudentCards.CollectionChanged -= handler
                    )
                )
                .Subscribe
                (
                    pattern => {
                        this.ReadStudents = new List<StudentCard>
                        (
                            studentCardService.ReadStudentCards
                        );
                    }
                );
        }


        public ObservableRangeCollection<object> AvailableGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> ChoseGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedAvailableGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedChoseGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ButtonConfig SelectGroups { get; set; }
        public ButtonConfig DeselectGroups { get; set; }

        public ButtonConfig SwitchViewButtonConfig { get; set; }
        public ButtonConfig SaveButtonConfig { get; set; }
        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public StudentModel Student { get; set; }
        [Reactive] public List<StudentCard> ReadStudents { get; set; } = new List<StudentCard>();

        public Dictionary<string, ListSortDirection> Sorts { get; set; } = new Dictionary<string, ListSortDirection> {
            {"Name", ListSortDirection.Ascending}
        };

        public Dictionary<string, ListSortDirection> GroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {"Group.Name", ListSortDirection.Ascending}
            };

        [Reactive] public StudentCard SelectedStudentCard { get; set; }

        [Reactive] public bool IsShowPhoto { get; set; }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public override Task Init() {
            Select<StudentModel>(this.Id , "Student")
                .Subscribe
                (
                    model => {
                        if (model == null)
                            return;
                        _originalStudent = model;
                        this.Student = new StudentModel(model);
                        this.ChoseGroups.Clear();
                        model.Groups
                            .ToList()
                            .ForEach
                            (
                                view => this.ChoseGroups.Add
                                (
                                    new ChoseGroupModel(view) {
                                        IsPraepostor = view.Chief != null && view.Chief.Id == model.Id,
                                        IsPraepostorAlreadySet = view.Chief != null && view.Chief.Id > 0
                                    }
                                )
                            );
                        var groupModels = _db.GroupModels.Include(group => @group.Students)
                            .AsEnumerable()
                            .Where
                            (
                                groupModel => groupModel._IsActive == 1
                                              && model.Groups.All
                                              (
                                                  studentGroup
                                                      => studentGroup.Id != groupModel.Id
                                              )
                            )
                            .ToList();
                        this.AvailableGroups.Clear();
                        this.AvailableGroups.AddRange(groupModels);
                        this.IsShowPhoto = model.Id != 0;
                    }
                );

            return Task.CompletedTask;
        }

        public class ChoseGroupModel {
            public ChoseGroupModel(GroupModel group) {
                this.Group = group;
            }

            public GroupModel Group { get; }
            public bool IsPraepostor { get; set; }

            public bool IsPraepostorAlreadySet { get; set; }
        }

        public ValidationContext ValidationContext { get; } = new ValidationContext();
    }
}
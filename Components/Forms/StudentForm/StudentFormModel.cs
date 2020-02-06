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
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Containers;
using DynamicData;
using DynamicData.Binding;
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
using TeacherAssistant.Utils;

namespace TeacherAssistant.StudentForm {
    public class StudentFormModel : AbstractModel<StudentFormModel>, IValidatableViewModel {
        public static readonly string LocalizationKey = "student.form";
        private readonly StudentFormToken _token;
        private PhotoService _photoService;
        private readonly LocalDbContext _context;
        private StudentEntity _originalStudent;

        public StudentFormModel(
            StudentFormToken token,
            PhotoService photoService,
            StudentCardService studentCardService,
            LocalDbContext context
        ) {
            _token = token;
            _photoService = photoService;
            _context = context;
            this.ChosenGroupTableConfig = new TableConfig {
                Sorts = ChosenGroupSorts
            };
            this.AvailableGroupTableConfig = new TableConfig {
                Sorts = AvailableGroupSorts
            };
            this.SelectGroupsHandler = ReactiveCommand.Create(SelectGroups);
            this.DeselectGroupsHandler = ReactiveCommand.Create(DeselectGroups);
            this.SwitchViewHandler = ReactiveCommand.Create(() => this.IsShowPhoto = !this.IsShowPhoto);
            this.SaveHandler = ReactiveCommand.Create(Save);
            this.WhenActivated(disposable => {
                this.WhenAnyValue(model => model.IsShowPhoto)
                    .CombineLatest
                    (
                        this.WhenAnyValue(model => model.Student)
                            .Select(student => student?.CardUid)
                            .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 6), LambdaHelper.ToTuple
                    )
                    .Throttle(TimeSpan.FromMilliseconds(500))
                    .Subscribe(SwitchView)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.SelectedStudentCard)
                    .Subscribe(ReadCard)
                    .DisposeWith(disposable);
                studentCardService.ReadStudentCards.ToObservableChangeSet()
                    .ToCollection()
                    .Subscribe(onNext: items => this.ReadStudents = items.ToList())
                    .DisposeWith(disposable);
            });
            Initialize(token.Student);
        }

        private void Initialize(StudentEntity entity) {
            if (entity == null)
                return;
            _originalStudent = entity;
            this.Student = new StudentEntity(entity);
            this.ChoseGroups.Clear();
            var choseGroupModels = entity.Groups.Select(group => new ChoseGroupModel(group) {
                IsPraepostor = group.Chief != null && group.Chief.Id == entity.Id,
                IsPraepostorAlreadySet = group.Chief != null && group.Chief.Id > 0
            }).ToList();
            this.ChoseGroups.AddRange(choseGroupModels);
            var groupModels = _context.Groups.Include(group => group.Students)
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

        private async void SwitchView(Tuple<bool, string> tuple) {
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

        private void ReadCard(StudentCard card) {
            if (card == null) {
                return;
            }

            this.Student.CardUid = card.CardUid;
            this.Student.FirstName = card.FirstName;
            this.Student.LastName = card.LastName;
            this.Student.SecondName = card.SecondName;
        }

        private void Save() {
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

            _context.SaveChangesAsync();
            _token.Deactivate();
        }

        private void SelectGroups() {
            foreach (var selectedAvailableGroup in this.SelectedAvailableGroups.Cast<GroupEntity>().ToList()) {
                this.AvailableGroups.Remove(selectedAvailableGroup);
                var choseGroupModel = new ChoseGroupModel(selectedAvailableGroup);
                this.ChoseGroups.Add(choseGroupModel);
            }
        }
        private void DeselectGroups() {
            foreach (var selectedChoseGroup in this.SelectedChoseGroups.Cast<ChoseGroupModel>().ToList()) {
                this.ChoseGroups.Remove(selectedChoseGroup);
                this.AvailableGroups.Add(selectedChoseGroup.Group);
            }
        }

        public TableConfig ChosenGroupTableConfig { get; set; }
        public TableConfig AvailableGroupTableConfig { get; set; }

        private ObservableCollection<object> AvailableGroups => this.AvailableGroupTableConfig.TableItems;

        private ObservableCollection<object> ChoseGroups => this.ChosenGroupTableConfig.TableItems;

        private IEnumerable<object> SelectedAvailableGroups => this.AvailableGroupTableConfig.SelectedItems;

        private IEnumerable<object> SelectedChoseGroups => this.ChosenGroupTableConfig.SelectedItems;

        public ICommand SelectGroupsHandler { get; set; }
        public ICommand DeselectGroupsHandler { get; set; }

        public ICommand SwitchViewHandler { get; set; }
        public ICommand SaveHandler { get; set; }
        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public StudentEntity Student { get; set; }
        [Reactive] public List<StudentCard> ReadStudents { get; set; } = new List<StudentCard>();

        private static Dictionary<string, ListSortDirection> AvailableGroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {nameof(GroupEntity.Name), ListSortDirection.Ascending}
            };

        private static Dictionary<string, ListSortDirection> ChosenGroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {string.Join(".", nameof(ChoseGroupModel.Group), nameof(GroupEntity.Name)), ListSortDirection.Ascending}
            };

        [Reactive] public StudentCard SelectedStudentCard { get; set; }

        [Reactive] public bool IsShowPhoto { get; set; }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public ValidationContext ValidationContext { get; } = new ValidationContext();
    }
}
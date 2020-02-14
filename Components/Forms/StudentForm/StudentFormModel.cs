using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Containers;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
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
        private StudentEntity _persistantStudent;

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
                Filter = (o, s) => ((ChoseGroupModel) o).Group.Name.ToUpperInvariant().Contains(s),
                Sorts = ChosenGroupSorts,
                ColumnWidths = new[] {
                    new GridLength(40),
                    new GridLength(1, GridUnitType.Star),
                }
            };
            this.AvailableGroupTableConfig = new TableConfig {
                Filter = (o, s) => ((GroupEntity) o).Name.ToUpperInvariant().Contains(s),
                Sorts = AvailableGroupSorts,
                ColumnWidths = new[] {
                    new GridLength(1, GridUnitType.Star),
                }
            };
            this.SelectGroupsHandler = ReactiveCommand.Create(SelectGroups);
            this.DeselectGroupsHandler = ReactiveCommand.Create(DeselectGroups);
            this.SelectStudentCardHandler = ReactiveCommand.Create(ReadCard);
            this.ValidationRule(model => model.CardUid,
                s => { return s == null || s.Equals(string.Empty) || StudentEntity.IsCardUidValid(s); },
                s => Localization["Неверный Uid"]);
            this.ValidationRule(model => model.FirstName, s => !string.IsNullOrWhiteSpace(s),
                Localization["Имя не может быть пустым"]);
            this.ValidationRule(model => model.LastName, s => !string.IsNullOrWhiteSpace(s),
                Localization["Имя не может быть пустым"]);
            this.SaveHandler = ReactiveCommand.Create(Save);
            this.WhenActivated(disposable => {
                this.IsValid().Subscribe(b => this.IsValid = b);
                this.WhenAnyValue(model => model.Student)
                    .Where(LambdaHelper.NotNull)
                    .Subscribe(entity => {
                        this.FirstName = entity.FirstName;
                        this.SecondName = entity.SecondName;
                        this.LastName = entity.LastName;
                        this.CardUid = entity.CardUid;
                        this.Email = entity.Email;
                        this.PhoneNumber = entity.PhoneNumber;
                    })
                    .DisposeWith(disposable);
                studentCardService.ReadStudentCards.ToObservableChangeSet()
                    .ToCollection()
                    .Subscribe(onNext: items => this.ReadStudents = items.ToList())
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.FirstName).Subscribe(s => this.Student.FirstName = s)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.LastName).Subscribe(s => this.Student.LastName = s)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.SecondName).Subscribe(s => this.Student.SecondName = s)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.CardUid).Subscribe(s => {
                        var isCardUidValid = StudentEntity.IsCardUidValid(s);
                        if (!isCardUidValid) {
                            this.IsShowPhoto = false;
                        }
                        else {
                            ShowPhotoAsync(s);
                            this.IsShowPhoto = true;
                        }

                        this.Student.CardUid = s;
                    })
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.PhoneNumber).Subscribe(s => this.Student.PhoneNumber = s)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.Email).Subscribe(s => this.Student.Email = s)
                    .DisposeWith(disposable);
            });
            Initialize(token.Student);
        }

        private void Initialize(StudentEntity entity) {
            if (entity == null)
                return;
            _persistantStudent = entity.Id == default ? entity : _context.Students.Find(entity.Id);
            this.Student = new StudentEntity(entity);
            this.ChosenGroups.Clear();
            var choseGroupModels = entity.Groups?.Select(group => new ChoseGroupModel(group) {
                IsPraepostor = group.Chief != null && group.Chief.Id == entity.Id,
                IsPraepostorAlreadySet = group.Chief != null && group.Chief.Id > 0
            }).ToList() ?? new List<ChoseGroupModel>();
            this.ChosenGroups.AddRange(choseGroupModels);
            var groupModels = _context.Groups.Include(group => group.Students)
                .AsEnumerable()
                .Where
                (
                    group => group._IsActive > 0
                             && (entity.Groups?.All(studentGroup => studentGroup.Id != group.Id) ?? true)
                )
                .ToList();
            this.AvailableGroups.Clear();
            this.AvailableGroups.AddRange(groupModels);
        }

        private async void ShowPhotoAsync([NotNull] string cardUid) {
            var path = await _photoService.DownloadPhoto(StudentEntity.CardUidToId(cardUid));
            if (string.IsNullOrEmpty(path))
                return;
            var photo = _photoService.GetImage(path);
            RunInUiThread(() => { this.StudentPhoto = photo; });
        }

        private void ReadCard() {
            var card = this.SelectedStudentCard;
            if (card == null) {
                return;
            }

            this.CardUid = card.CardUid;
            this.FirstName = string.IsNullOrWhiteSpace(this.FirstName) ? card.FirstName : this.FirstName;
            this.LastName = string.IsNullOrWhiteSpace(this.LastName) ? card.LastName : this.LastName;
            this.SecondName = string.IsNullOrWhiteSpace(this.SecondName) ? card.SecondName : this.SecondName;
        }

        private async Task Save() {
            var isNew = this.Student.Id == default;
            this.Student.Groups =
                this.ChosenGroups.Cast<ChoseGroupModel>().Select(model => model.Group).ToList();
            _persistantStudent.Apply(this.Student);
            if (isNew) {
                _context.Students.Add(_persistantStudent);
            }

            foreach (ChoseGroupModel selectedChoseGroup in this.ChosenGroups) {
                if (selectedChoseGroup.IsPraepostor) {
                    selectedChoseGroup.Group.Chief = _persistantStudent;
                }
                else {
                    if (selectedChoseGroup.Group._PraepostorId == _persistantStudent.Id) {
                        selectedChoseGroup.Group.Chief = null;
                    }
                }
            }

            await _context.SaveChangesAsync();
            _token.Deactivate();
        }

        private void SelectGroups() {
            foreach (var selectedAvailableGroup in this.SelectedAvailableGroups.Cast<GroupEntity>().ToList()) {
                this.AvailableGroups.Remove(selectedAvailableGroup);
                var choseGroupModel = new ChoseGroupModel(selectedAvailableGroup);
                this.ChosenGroups.Add(choseGroupModel);
            }
        }

        private void DeselectGroups() {
            foreach (var selectedChoseGroup in this.SelectedChoseGroups.Cast<ChoseGroupModel>().ToList()) {
                this.ChosenGroups.Remove(selectedChoseGroup);
                this.AvailableGroups.Add(selectedChoseGroup.Group);
            }
        }


        private ObservableCollection<object> AvailableGroups => this.AvailableGroupTableConfig.TableItems;

        private ObservableCollection<object> ChosenGroups => this.ChosenGroupTableConfig.TableItems;

        private IEnumerable<object> SelectedAvailableGroups => this.AvailableGroupTableConfig.SelectedItems;

        private IEnumerable<object> SelectedChoseGroups => this.ChosenGroupTableConfig.SelectedItems;
        [NotNull] public TableConfig ChosenGroupTableConfig { get; set; }
        [NotNull] public TableConfig AvailableGroupTableConfig { get; set; }

        [NotNull] public ICommand SelectStudentCardHandler { get; set; }
        [NotNull] public ICommand SelectGroupsHandler { get; set; }
        [NotNull] public ICommand DeselectGroupsHandler { get; set; }
        [NotNull] public ICommand SaveHandler { get; set; }
        [Reactive] [CanBeNull] public BitmapImage StudentPhoto { get; set; }
        [Reactive] [NotNull] public StudentEntity Student { get; set; }
        [Reactive] [NotNull] public List<StudentCard> ReadStudents { get; set; } = new List<StudentCard>();

        [Reactive] public bool IsValid { get; set; } = true;

        [Reactive] [CanBeNull] public string FirstName { get; set; }
        [Reactive] [CanBeNull] public string LastName { get; set; }
        [Reactive] [CanBeNull] public string SecondName { get; set; }
        [Reactive] [CanBeNull] public string CardUid { get; set; }
        [Reactive] [CanBeNull] public string PhoneNumber { get; set; }
        [Reactive] [CanBeNull] public string Email { get; set; }

        [Reactive] [CanBeNull] public StudentCard SelectedStudentCard { get; set; }

        [Reactive] public bool IsShowPhoto { get; set; }

        private static Dictionary<string, ListSortDirection> AvailableGroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {nameof(GroupEntity.Name), ListSortDirection.Ascending}
            };

        private static Dictionary<string, ListSortDirection> ChosenGroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {string.Join(".", nameof(ChoseGroupModel.Group), nameof(GroupEntity.Name)), ListSortDirection.Ascending}
            };

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Containers;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.Core.Module;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;

namespace TeacherAssistant.Forms.StreamForm
{
    public class StreamFormModel : AbstractModel<StreamFormModel>
    {
        private readonly ModuleActivation<StreamFormToken> _activation;
        private readonly LocalDbContext _context;

        private static DropDownItem<int> DefaultCourse =
            new DropDownItem<int>(Localization["Пусто"], -1);

        private static DepartmentEntity DefaultDepartment = new DepartmentEntity
            {Id = -1, Name = Localization["Пусто"]};

        [NotNull] private StreamEntity _entity;

        public StreamFormModel(ModuleActivation<StreamFormToken> activation, LocalDbContext context)
        {
            _activation = activation;
            _context = context;
            this.SaveHandler = ReactiveCommand.Create(Save);
            this.AddGroupsHandler = ReactiveCommand.Create(SelectGroups);
            this.RemoveGroupsHandler = ReactiveCommand.Create(DeselectGroups);
            this.AvailableGroupTableConfig = new TableConfig
            {
                Filter = GroupFilter,
                Sorts = GroupSorts,
                ColumnWidths = new[]
                {
                    new GridLength(1, GridUnitType.Star),
                }
            };
            this.ChosenGroupTableConfig = new TableConfig
            {
                Filter = GroupFilter,
                Sorts = GroupSorts,
                ColumnWidths = new[]
                {
                    new GridLength(1, GridUnitType.Star),
                }
            };
            Initialize(activation.Token.Stream);
        }

        private void Initialize(StreamEntity stream)
        {
            if (stream == null)
            {
                return;
            }

            this.AvailableCourses.Clear();
            var dropDownItems = Enumerable.Range(1, 5).Select(i => new DropDownItem<int>(i.ToString(), i)).ToList();
            this.AvailableCourses.Add(DefaultCourse);
            this.AvailableCourses.AddRange(dropDownItems);
            this.Disciplines.Clear();
            this.Departments.Clear();
            this.Disciplines.AddRange(_context.Disciplines.ToList());
            this.Departments.Add(DefaultDepartment);
            this.Departments.AddRange(_context.Departments.ToList());

            _entity = stream.Id == default ? stream : _context.Streams.Find(stream.Id) ?? stream;
            this.ChosenGroups.AddRange(_entity.Groups?.ToList() ?? new List<GroupEntity>());
            this.AvailableGroups.AddRange(_context.Groups.ToList()
                .Where(groupModel => _entity.Groups?.All(o => groupModel.Id != o.Id) ?? true).ToList());
            this.StreamName = _entity.Name;
            this.IsActive = _entity.IsActive;
            this.ExpirationDate = _entity.ExpirationDate ?? DateTime.Today;
            this.SelectedCourse =
                this.AvailableCourses.FirstOrDefault(item => item.Value.Equals(_entity.Course)) ??
                DefaultCourse;
            this.SelectedDiscipline =
                this.Disciplines.FirstOrDefault(discipline => discipline.Id == _entity._DisciplineId) ??
                this.Disciplines.FirstOrDefault();
            this.SelectedDepartment =
                this.Departments.FirstOrDefault(department => department.Id == _entity._DepartmentId) ??
                DefaultDepartment;
            this.LaboratoryCount = _entity.LabCount;
            this.LectureCount = _entity.LectureCount;
            this.PracticeCount = _entity.PracticalCount;
            this.Description = _entity.Description;
        }

        public TableConfig AvailableGroupTableConfig { get; set; }
        public TableConfig ChosenGroupTableConfig { get; set; }

        public ICommand SaveHandler { get; set; }
        public ICommand AddGroupsHandler { get; set; }
        public ICommand RemoveGroupsHandler { get; set; }

        public ObservableCollection<DisciplineEntity> Disciplines { get; set; } =
            new ObservableCollection<DisciplineEntity>();

        public ObservableCollection<DepartmentEntity> Departments { get; set; } =
            new ObservableCollection<DepartmentEntity>();

        public ObservableCollection<DropDownItem<int>> AvailableCourses { get; set; } =
            new ObservableCollection<DropDownItem<int>>();

        public ObservableCollection<object> AvailableGroups => this.AvailableGroupTableConfig.TableItems;
        public ObservableCollection<object> ChosenGroups => this.ChosenGroupTableConfig.TableItems;

        public ObservableCollection<object> SelectedAvailableGroups => this.AvailableGroupTableConfig.SelectedItems;

        public ObservableCollection<object> SelectedChosenGroups => this.ChosenGroupTableConfig.SelectedItems;

        private static Dictionary<string, ListSortDirection> GroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection>
            {
                {"Name", ListSortDirection.Ascending}
            };

        [Reactive]
        public DisciplineEntity SelectedDiscipline { get; set; }

        [Reactive]
        [NotNull]
        public DepartmentEntity SelectedDepartment { get; set; } = DefaultDepartment;

        [Reactive]
        [CanBeNull]
        public string StreamName { get; set; }

        [Reactive]
        public bool IsActive { get; set; } = false;

        [Reactive]
        public DateTime ExpirationDate { get; set; } = DateTime.Now;

        [Reactive]
        [CanBeNull]
        public string Description { get; set; }

        [Reactive]
        [NotNull]
        public DropDownItem<int> SelectedCourse { get; set; } = DefaultCourse;

        [Reactive]
        public int LectureCount { get; set; } = 0;

        [Reactive]
        public int PracticeCount { get; set; } = 0;

        [Reactive]
        public int LaboratoryCount { get; set; } = 0;

        private async Task Save()
        {
            _entity.Name = this.StreamName;
            _entity.Groups = this.ChosenGroups.Cast<GroupEntity>().ToList();
            _entity.Discipline = this.SelectedDiscipline;
            _entity.Department = this.SelectedDepartment.Id == DefaultDepartment.Id ? null : this.SelectedDepartment;
            _entity.ExpirationDate = this.ExpirationDate;
            _entity.IsActive = this.IsActive;
            _entity.LectureCount = this.LectureCount;
            _entity.LabCount = this.LaboratoryCount;
            _entity.PracticalCount = this.PracticeCount;
            _entity.Description = this.Description;
            _entity.Course = this.SelectedCourse.Value == DefaultCourse.Value ? (int?) null : this.SelectedCourse.Value;
            if (_entity.Id == default)
            {
                _context.Streams.Add(_entity);
            }

            await _context.SaveChangesAsync();
            _activation.Deactivate();
        }

        private void SelectGroups()
        {
            var selected = this.SelectedAvailableGroups.ToList();
            this.ChosenGroups.AddRange(selected);
            this.AvailableGroups.RemoveMany(selected);
        }

        private void DeselectGroups()
        {
            var selected = this.SelectedChosenGroups.ToList();
            this.AvailableGroups.AddRange(selected);
            this.ChosenGroups.RemoveMany(selected);
        }

        private static Func<object, string, bool> GroupFilter = (item, searchValue) =>
            ((GroupEntity) item).Name?.ToUpper().Contains(searchValue) ?? false;

        protected override string GetLocalizationKey()
        {
            return "stream.form";
        }
    }
}
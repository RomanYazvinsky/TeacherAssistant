using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Containers;
using Model;
using Model.Models;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Forms.StreamForm {
    public class StreamFormModel : AbstractModel {
        private static DropDownItem<int> DefaultCourse =
            new DropDownItem<int>(Localization["common.empty.dropdown"], -1);

        private static DepartmentEntity DefaultDepartment = new DepartmentEntity
            {Id = -1, Name = Localization["common.empty.dropdown"]};

        private StreamEntity _entity;

        public StreamFormModel(string id) : base(id) {
            this.SaveHandler = new CommandHandler(Save);
            this.AddGroupsHandler = new CommandHandler(SelectGroups);
            this.RemoveGroupsHandler = new CommandHandler(DeselectGroups);
            this.AvailableGroupTableConfig = new TableConfig {
                Filter = this.GroupFilter,
                Sorts = GroupSorts
            };
            this.ChosenGroupTableConfig = new TableConfig {
                Filter = this.GroupFilter,
                Sorts = GroupSorts
            };
            Select<StreamEntity>(this.Id, "StreamChange").Subscribe(Initialize);
        }

        private void Initialize(StreamEntity stream) {
            if (stream == null) {
                return;
            }
            this.AvailableCourses.Clear();
            var dropDownItems = Enumerable.Range(1, 5).Select(i => new DropDownItem<int>(i.ToString(), i)).ToList();
            this.AvailableCourses.Add(DefaultCourse);
            this.AvailableCourses.AddRange(dropDownItems);
            this.Disciplines.Clear();
            this.Departments.Clear();
            this.Disciplines.AddRange(_db.Disciplines.ToList());
            this.Departments.Add(DefaultDepartment);
            this.Departments.AddRange(_db.Departments.ToList());

            _entity = stream;
            this.ChosenGroups.AddRange(stream.Groups.ToList());
            this.AvailableGroups.AddRange(_db.Groups
                .Where(groupModel => stream.Groups.All(o => groupModel.Id != o.Id)).ToList());
            this.StreamName = stream.Name;
            this.IsActive = stream.IsActive;
            this.ExpirationDate = stream.ExpirationDate ?? DateTime.Today;
            this.SelectedCourse =
                this.AvailableCourses.FirstOrDefault(item => item.Value.Equals(stream.Course)) ??
                DefaultCourse;
            this.SelectedDiscipline =
                this.Disciplines.FirstOrDefault(discipline => discipline.Id == stream._DisciplineId) ??
                this.Disciplines.FirstOrDefault();
            this.SelectedDepartment =
                this.Departments.FirstOrDefault(department => department.Id == stream._DepartmentId) ??
                DefaultDepartment;
        }

        public TableConfig AvailableGroupTableConfig { get; set; }
        public TableConfig ChosenGroupTableConfig { get; set; }
        
        public CommandHandler SaveHandler { get; set; }
        public CommandHandler AddGroupsHandler { get; set; }
        public CommandHandler RemoveGroupsHandler { get; set; }

        public ObservableRangeCollection<DisciplineEntity> Disciplines { get; set; } =
            new WpfObservableRangeCollection<DisciplineEntity>();

        public ObservableRangeCollection<DepartmentEntity> Departments { get; set; } =
            new WpfObservableRangeCollection<DepartmentEntity>();

        public ObservableRangeCollection<DropDownItem<int>> AvailableCourses { get; set; } =
            new WpfObservableRangeCollection<DropDownItem<int>>();

        public ObservableRangeCollection<object> AvailableGroups => this.AvailableGroupTableConfig.TableItems;
        public ObservableRangeCollection<object> ChosenGroups => this.ChosenGroupTableConfig.TableItems;

        public ObservableRangeCollection<object> SelectedAvailableGroups => this.AvailableGroupTableConfig.SelectedItems;

        public ObservableRangeCollection<object> SelectedChosenGroups => this.ChosenGroupTableConfig.SelectedItems;

        private static Dictionary<string, ListSortDirection> GroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {"Name", ListSortDirection.Ascending}
            };

        [Reactive] public DisciplineEntity SelectedDiscipline { get; set; }
        [Reactive] public DepartmentEntity SelectedDepartment { get; set; } = DefaultDepartment;

        [Reactive] public string StreamName { get; set; }
        [Reactive] public bool IsActive { get; set; } = false;
        [Reactive] public DateTime ExpirationDate { get; set; } = DateTime.Now;
        [Reactive] public DropDownItem<int> SelectedCourse { get; set; } = DefaultCourse;
        [Reactive] public int LectureCount { get; set; } = 0;
        [Reactive] public int PracticeCount { get; set; } = 0;
        [Reactive] public int LaboratoryCount { get; set; } = 0;

        private void Save() {
            _entity.Name = this.StreamName;
            _entity.Groups = this.ChosenGroups.Cast<GroupEntity>().ToList();
            _entity.Discipline = this.SelectedDiscipline;
            _entity.Department = this.SelectedDepartment.Id == DefaultDepartment.Id ? null : this.SelectedDepartment;
            _entity.ExpirationDate = this.ExpirationDate;
            _entity.IsActive = this.IsActive;
            _entity.LectureCount = this.LectureCount;
            _entity.LabCount = this.LaboratoryCount;
            _entity.PracticalCount = this.PracticeCount;
            _entity.Course = this.SelectedCourse.Value == DefaultCourse.Value ? 0 : DefaultCourse.Value;
            if (_entity.Id == 0) {
                _db.Streams.Add(_entity);
            }
            _db.SaveChangesAsync();
        }

        private void SelectGroups() {
            var selected = this.SelectedAvailableGroups.ToList();
            this.ChosenGroups.AddRange(selected);
            this.AvailableGroups.RemoveRange(selected);
        }

        private void DeselectGroups() {
            var selected = this.SelectedChosenGroups.ToList();
            this.AvailableGroups.AddRange(selected);
            this.ChosenGroups.RemoveRange(selected);
        }

        public Func<object, string, bool> GroupFilter { get; set; } = (item, searchValue) =>
            ((GroupEntity) item).Name?.ToUpper().Contains(searchValue.ToUpper()) ?? false;

        protected override string GetLocalizationKey() {
            return "stream.form";
        }
    }
}
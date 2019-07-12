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
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Forms.StreamForm {
    public class StreamFormModel : AbstractModel {
        private static DropDownItem<int> DefaultCourse =
            new DropDownItem<int>(Localization["common.empty.dropdown"], -1);

        private static DepartmentModel DefaultDepartment = new DepartmentModel
            {Id = -1, Name = Localization["common.empty.dropdown"]};

        private StreamModel _model;

        public StreamFormModel(string id) : base(id) {
            this.SaveHandler = new CommandHandler(Save);
            this.AddGroupsHandler = new CommandHandler(SelectGroups);
            this.RemoveGroupsHandler = new CommandHandler(DeselectGroups);
            var dropDownItems = Enumerable.Range(1, 5).Select(i => new DropDownItem<int>(i.ToString(), i)).ToList();
            this.AvailableCourses.Add(DefaultCourse);
            this.AvailableCourses.AddRange(dropDownItems);
            this.Disciplines.AddRange(_db.DisciplineModels.ToList());
            this.Departments.Add(DefaultDepartment);
            this.Departments.AddRange(_db.DepartmentModels.ToList());
        }

        public CommandHandler SaveHandler { get; set; }
        public CommandHandler AddGroupsHandler { get; set; }
        public CommandHandler RemoveGroupsHandler { get; set; }

        public ObservableRangeCollection<DisciplineModel> Disciplines { get; set; } =
            new WpfObservableRangeCollection<DisciplineModel>();

        public ObservableRangeCollection<DepartmentModel> Departments { get; set; } =
            new WpfObservableRangeCollection<DepartmentModel>();

        public ObservableRangeCollection<DropDownItem<int>> AvailableCourses { get; set; } =
            new WpfObservableRangeCollection<DropDownItem<int>>();

        public ObservableRangeCollection<object> AvailableGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> ChosenGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedAvailableGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedChosenGroups { get; set; } =
            new WpfObservableRangeCollection<object>();

        public Dictionary<string, ListSortDirection> GroupSorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {"Name", ListSortDirection.Ascending}
            };

        [Reactive] public DisciplineModel SelectedDiscipline { get; set; }
        [Reactive] public DepartmentModel SelectedDepartment { get; set; } = DefaultDepartment;

        [Reactive] public string StreamName { get; set; }
        [Reactive] public bool IsActive { get; set; } = false;
        [Reactive] public DateTime ExpirationDate { get; set; } = DateTime.Now;
        [Reactive] public DropDownItem<int> SelectedCourse { get; set; } = DefaultCourse;
        [Reactive] public int LectureCount { get; set; } = 0;
        [Reactive] public int PracticeCount { get; set; } = 0;
        [Reactive] public int LaboratoryCount { get; set; } = 0;

        private void Save() {
            _model.Name = this.StreamName;
            _model.Groups = this.ChosenGroups.Cast<GroupModel>().ToList();
            _model.Discipline = this.SelectedDiscipline;
            _model.Department = this.SelectedDepartment.Id == DefaultDepartment.Id ? null : this.SelectedDepartment;
            _model.ExpirationDate = this.ExpirationDate;
            _model.IsActive = this.IsActive;
            _model.LectureCount = this.LectureCount;
            _model.LabCount = this.LaboratoryCount;
            _model.PracticalCount = this.PracticeCount;
            _model.Course = this.SelectedCourse.Value == DefaultCourse.Value ? 0 : DefaultCourse.Value;
            if (_model.Id == 0) {
                _db.StreamModels.Add(_model);
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
            ((GroupModel) item).Name?.ToUpper().Contains(searchValue.ToUpper()) ?? false;

        protected override string GetLocalizationKey() {
            return "stream.form";
        }

        public override Task Init() {
            Select<StreamModel>(this.Id, "StreamChange").Subscribe(
                model => {
                    if (model == null) {
                        return;
                    }

                    _model = model;
                    this.ChosenGroups.AddRange(model.Groups.ToList());
                    this.AvailableGroups.AddRange(_db.GroupModels
                        .Where(groupModel => model.Groups.All(o => groupModel.Id != o.Id)).ToList());
                    this.StreamName = model.Name;
                    this.IsActive = model.IsActive;
                    this.ExpirationDate = model.ExpirationDate ?? DateTime.Today;
                    this.SelectedCourse =
                        this.AvailableCourses.FirstOrDefault(item => item.Value.Equals(model.Course)) ??
                        DefaultCourse;
                    this.SelectedDiscipline =
                        this.Disciplines.FirstOrDefault(discipline => discipline.Id == model._DisciplineId) ??
                        this.Disciplines.FirstOrDefault();
                    this.SelectedDepartment =
                        this.Departments.FirstOrDefault(department => department.Id == model._DepartmentId) ??
                        DefaultDepartment;
                });
            return Task.CompletedTask;
        }
    }
}
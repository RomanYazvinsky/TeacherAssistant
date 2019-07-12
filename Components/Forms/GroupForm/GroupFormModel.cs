using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Containers;
using Model;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.ComponentsImpl;

namespace TeacherAssistant.Forms.GroupForm {
    public class GroupFormModel : AbstractModel {
        private List<StudentModel> _students = new List<StudentModel>();
        private bool isValid;

        public GroupFormModel(string id) : base(id) {
            this.StudentFilter = (o, s) => {
                var upperString = s.ToUpper();
                var student = ((StudentListItem) o).Student;
                return student.FirstName.ToUpper().Contains(upperString)
                       || student.LastName.ToUpper().Contains(upperString)
                       || student.SecondName.ToUpper().Contains(upperString);
            };
            this.AddStudentsToGroupConfig = new ButtonConfig {
                Command = new CommandHandler(AddStudentsToGroup)
            };
            this.RemoveStudentsFromGroupConfig = new ButtonConfig {
                Command = new CommandHandler(RemoveStudentsFromGroup)
            };
            this.SaveGroupConfig = new ButtonConfig {
                Command = new CommandHandler(Save)
            };
            this.WhenAnyValue
                 (model => model.SelectedDepartment)
                .Where(department => NotNull(department) && department.Id > 0)
                .Subscribe(department => this.Group.Department = department);

            this.WhenAnyValue(model => model.Group.Name)
                .Subscribe
                 (
                     s => { isValid = !string.IsNullOrWhiteSpace(s); }
                 );
        }

        public ObservableRangeCollection<DepartmentModel> Departments { get; set; } =
            new WpfObservableRangeCollection<DepartmentModel>();

        public ObservableRangeCollection<object> Students { get; set; } = new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> GroupStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        [Reactive] public DepartmentModel SelectedDepartment { get; set; }
        [Reactive] public GroupModel Group { get; set; }

        public ObservableRangeCollection<object> ChiefStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedGroupStudents { get; set; } =
            new WpfObservableRangeCollection<object>();


        public Dictionary<string, ListSortDirection> Sorts { get; set; } =
            new Dictionary<string, ListSortDirection> {
                {"Name", ListSortDirection.Ascending}
            };

        public Func<object, string, bool> StudentFilter { get; set; }

        public ButtonConfig AddStudentsToGroupConfig { get; set; }
        public ButtonConfig RemoveStudentsFromGroupConfig { get; set; }
        public ButtonConfig SaveGroupConfig { get; set; }

        private void AddStudentsToGroup() {
            var selectedGroupStudents = this.SelectedStudents.ToList();
            foreach (var selectedGroupStudent in selectedGroupStudents) {
                this.GroupStudents.Add(selectedGroupStudent);
                this.Students.Remove(selectedGroupStudent);
            }
        }

        private void RemoveStudentsFromGroup() {
            var selectedGroupStudents = this.SelectedGroupStudents.ToList();
            foreach (var selectedGroupStudent in selectedGroupStudents) {
                this.GroupStudents.Remove(selectedGroupStudent);
                this.Students.Add(selectedGroupStudent);
            }
        }


        protected override string GetLocalizationKey() {
            return "group.form";
        }

        private void Save() {
            if (!isValid) {
                return;
            }

            this.Group.Students =
                this.GroupStudents.Cast<StudentListItem>().Select(item => item.Student).ToList();
            if (this.Group.Id == 0) {
                _db.GroupModels.Add(this.Group);
            }
            else {
                var group = _db.GroupModels.Find(this.Group.Id);
                group.Apply(this.Group);
            }

            _db.SaveChangesAsync();
            this.PageService.ClosePage(this.Id);
        }

        public override Task Init() {
            Select<GroupModel>(this.Id, "GroupChange")
               .Subscribe
                (
                    async group => {
                        if (group == null) {
                            return;
                        }

                        this.Group = group.Id == 0 ? group : new GroupModel(group);
                        var departments = await _db.DepartmentModels.ToListAsync();
                        departments.Insert
                        (
                            0,
                            new DepartmentModel {
                                Id = -1, Name = Localization["dropdown.empty"]
                            }
                        );
                        this.Departments.Clear();
                        this.Departments.AddRange(departments);
                        this.SelectedDepartment = this.Group.Department ?? this.Departments[0];
                        this.Students.Clear();
                        _students = await _db.StudentModels.Include("Groups").ToListAsync();
                        _students.Sort
                        (
                            (model, model2) => string.Compare
                            (
                                model.LastName.ToLower(),
                                model2.LastName.ToLower(),
                                StringComparison.InvariantCultureIgnoreCase
                            )
                        );
                        this.GroupStudents.AddRange
                            (group.Students.Select(model => new StudentListItem(model)).ToList());

                        var studentListItems = _students.Where
                                                         (model => group.Students.All(s => s.Id != model.Id))
                                                        .Select(model => new StudentListItem(model));
                        this.Students.AddRange(studentListItems);

                        this.ChiefStudents.Clear();
                        this.ChiefStudents.AddRange
                        (
                            _students.Select(model => new StudentDropdownItem(model)).ToList()
                        );
                    }
                );
            return Task.CompletedTask;
        }
    }

    public class StudentDropdownItem {
        public StudentDropdownItem(StudentModel student) {
            this.Student = student;
            this.Name = student.LastName
                        + " "
                        + student.FirstName
                        + " "
                        + student.SecondName;
        }

        public string Name { get; set; }
        public StudentModel Student { get; set; }
    }

    public class StudentListItem : StudentDropdownItem {
        public StudentListItem(StudentModel student) : base(student) {
            this.Groups = string.Join
            (
                ", ",
                student.Groups.Select
                (
                    groupModel => groupModel.Name
                )
            );
        }

        public string Groups { get; set; }
    }
}
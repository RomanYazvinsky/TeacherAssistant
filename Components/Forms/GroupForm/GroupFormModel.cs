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
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Modules.MainModule;

namespace TeacherAssistant.Forms.GroupForm {
    public class GroupFormModel : AbstractModel {
        private readonly IPageHost _pageHost;
        private readonly GroupFormToken _token;
        private readonly LocalDbContext _db;
        private List<StudentEntity> _students = new List<StudentEntity>();
        private bool isValid;

        private static readonly Dictionary<string, ListSortDirection> Sorts = new Dictionary<string, ListSortDirection> {
            {"Name", ListSortDirection.Ascending}
        };

        public GroupFormModel(TabPageHost pageHost, MainReducer reducer, GroupFormToken token, LocalDbContext db) {
            _db = db;
            _pageHost = pageHost;
            _token = token;
            this.Reducer = reducer;
            this.StudentsTableConfig = new TableConfig {
                Filter = (o, s) => {
                    var upperString = s.ToUpper();
                    var student = ((StudentListItem) o).Student;
                    return student.FirstName.ToUpper().Contains(upperString)
                           || student.LastName.ToUpper().Contains(upperString)
                           || student.SecondName.ToUpper().Contains(upperString);
                },
                Sorts = Sorts
            };
            this.GroupStudentsTableConfig = new TableConfig {
                Filter = (o, s) => {
                    var upperString = s.ToUpper();
                    var student = ((StudentListItem) o).Student;
                    return student.FirstName.ToUpper().Contains(upperString)
                           || student.LastName.ToUpper().Contains(upperString)
                           || student.SecondName.ToUpper().Contains(upperString);
                },
                Sorts = Sorts
            };
            this.Students = this.StudentsTableConfig.TableItems;
            this.SelectedStudents = this.StudentsTableConfig.SelectedItems;
            this.GroupStudents = this.GroupStudentsTableConfig.TableItems;
            this.SelectedGroupStudents = this.GroupStudentsTableConfig.TableItems;
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
            Init(token.Group);
        }

        
        private async void Init(GroupEntity group) {
            this.Group = group.Id == 0 ? group : new GroupEntity(group); // todo fill in parallel
            var departments = await _db.Departments.ToListAsync();
            departments.Insert
            (
                0,
                new DepartmentEntity {
                    Id = -1, Name = Localization["dropdown.empty"]
                }
            );
            this.Departments.Clear();
            this.Departments.AddRange(departments);
            this.SelectedDepartment = this.Group.Department ?? this.Departments[0];
            this.Students.Clear();
            _students = await _db.Students.Include("Groups").ToListAsync();
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

        public MainReducer Reducer { get; }
        
        public TableConfig StudentsTableConfig { get; set; }
        public TableConfig GroupStudentsTableConfig { get; set; }

        public ObservableRangeCollection<DepartmentEntity> Departments { get; set; } =
            new WpfObservableRangeCollection<DepartmentEntity>();

        public ObservableRangeCollection<object> Students { get; set; }

        public ObservableRangeCollection<object> GroupStudents { get; set; }
        [Reactive] public DepartmentEntity SelectedDepartment { get; set; }
        [Reactive] public GroupEntity Group { get; set; }

        public ObservableRangeCollection<object> ChiefStudents { get; set; } =
            new WpfObservableRangeCollection<object>();

        public ObservableRangeCollection<object> SelectedStudents { get; set; }

        public ObservableRangeCollection<object> SelectedGroupStudents { get; set; }

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
                _db.Groups.Add(this.Group);
            } else {
                var group = _db.Groups.Find(this.Group.Id);
                group?.Apply(this.Group);
            }

            _db.SaveChangesAsync();
            _pageHost.ClosePage(_token.Id);
        }
    }

    public class StudentDropdownItem {
        public StudentDropdownItem(StudentEntity student) {
            this.Student = student;
            this.Name = student.LastName
                        + " "
                        + student.FirstName
                        + " "
                        + student.SecondName;
        }

        public string Name { get; set; }
        public StudentEntity Student { get; set; }
    }

    public class StudentListItem : StudentDropdownItem {
        public StudentListItem(StudentEntity student) : base(student) {
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
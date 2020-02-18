using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Containers;
using DynamicData;
using JetBrains.Annotations;
using Model;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.Components.TableFilter;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.Modules.MainModule;
using TeacherAssistant.PageBase;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Forms.GroupForm
{
    public class GroupFormModel : AbstractModel<GroupFormModel>
    {
        private readonly GroupFormToken _token;
        private readonly LocalDbContext _db;
        [NotNull] private List<StudentEntity> _students = new List<StudentEntity>();

        private static readonly Dictionary<string, ListSortDirection> Sorts = new Dictionary<string, ListSortDirection>
        {
            {"Name", ListSortDirection.Ascending}
        };

        public GroupFormModel(GroupFormToken token, LocalDbContext db)
        {
            _db = db;
            _token = token;
            this.StudentsTableConfig = new TableConfig
            {
                Filter = FilterStudentNames,
                Sorts = Sorts,
                ColumnWidths = new []{new GridLength(1, GridUnitType.Star),new GridLength(1, GridUnitType.Star) }
            };
            this.GroupStudentsTableConfig = new TableConfig
            {
                Filter = FilterStudentNames,
                Sorts = Sorts,
                ColumnWidths = new []{new GridLength(1, GridUnitType.Star),new GridLength(1, GridUnitType.Star) }
            };
            this.Students = this.StudentsTableConfig.TableItems;
            this.SelectedStudents = this.StudentsTableConfig.SelectedItems;
            this.GroupStudents = this.GroupStudentsTableConfig.TableItems;
            this.SelectedGroupStudents = this.GroupStudentsTableConfig.SelectedItems;
            this.AddStudentsToGroupHandler = ReactiveCommand.Create(AddStudentsToGroup);
            this.RemoveStudentsFromGroupHandler = ReactiveCommand.Create(RemoveStudentsFromGroup);
            this.SaveGroupHandler = ReactiveCommand.Create(Save);
            NameValidation = this.ValidationRule(
                model => model.GroupName,
                s => !string.IsNullOrWhiteSpace(s),
                s => Localization["Неверное имя!"]);
            this.WhenActivated(disposable =>
            {
                this.IsValid().Subscribe(b => this.IsValid = b);
                this.WhenAnyValue(model => model.SelectedDepartment)
                    .Skip(1)
                    .Subscribe(department => this.Group.Department = department?.Id != default ? department : null)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.ExpirationDate)
                    .Skip(1)
                    .Subscribe(time => this.Group.ExpirationDate = time)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.IsGroupActive)
                    .Skip(1)
                    .Subscribe(isActive => this.Group.IsActive = isActive)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.GroupName)
                    .Skip(1)
                    .Subscribe(groupName => this.Group.Name = groupName)
                    .DisposeWith(disposable);
                this.WhenAnyValue(model => model.SelectedChiefStudent)
                    .Skip(1)
                    .Subscribe(chief => this.Group.Chief = chief?.Student)
                    .DisposeWith(disposable);
            });
            Init(token.Group);
        }


        private void Init(GroupEntity group)
        {
            this.Group = group.Id == default ? group : new GroupEntity(group);
            this.IsGroupActive = this.Group.IsActive;
            this.ExpirationDate = this.Group.ExpirationDate ?? DateTime.Now;
            this.GroupName = this.Group.Name;
            SetupDepartmentsAsync();
            SetupGroupStudents();
            SetupAvailableStudentsAsync();
        }

        private void SetupGroupStudents()
        {
            this.GroupStudents.Clear();
            var studentViewModels = this.Group.Students?
                                        .Select(model => new StudentViewModel(model)).ToList()
                                    ?? new List<StudentViewModel>();
            this.GroupStudents.AddRange(studentViewModels);
        }

        private async Task SetupAvailableStudentsAsync()
        {
            _students = await _db.Students.Include(entity => entity.Groups).ToListAsync();
            _students.Sort(CompareStudentNames);
            var studentListItems = this.Group.Students == null
                ? _students.Select(entity => new StudentViewModel(entity))
                : _students.Where(student => this.Group.Students.All(s => s.Id != student.Id))
                    .Select(model => new StudentViewModel(model));
            RunInUiThread(() =>
            {
                this.Students.Clear();
                this.Students.AddRange(studentListItems);
            });
        }

        private async Task SetupDepartmentsAsync()
        {
            var departments = await _db.Departments.ToListAsync();
            RunInUiThread(() =>
            {
                this.DepartmentSelectionAvailable = departments.Count > 0;
                departments.Insert
                (
                    0,
                    new DepartmentEntity
                    {
                        Id = -1, Name = Localization["(Пусто)"]
                    }
                );
                this.Departments.Clear();
                this.Departments.AddRange(departments);
                this.SelectedDepartment = this.Group.Department ?? this.Departments.First();
            });
        }

        private static int CompareStudentNames(StudentEntity s1, StudentEntity s2)
        {
            return string.Compare(
                s1.LastName.ToUpper(),
                s2.LastName.ToUpper(),
                StringComparison.InvariantCulture
            );
        }

        private static bool FilterStudentNames(object studentObject, string value)
        {
            var upperString = value.ToUpper();
            var student = ((StudentViewModel) studentObject).Student;
            return student.FirstName.ToUpper().Contains(upperString)
                   || student.LastName.ToUpper().Contains(upperString)
                   || student.SecondName.ToUpper().Contains(upperString);
        }

        public ValidationHelper NameValidation { get; set; }
        public TableConfig StudentsTableConfig { get; set; }
        public TableConfig GroupStudentsTableConfig { get; set; }

        public ObservableCollection<DepartmentEntity> Departments { get; set; } =
            new ObservableCollection<DepartmentEntity>();

        public ObservableCollection<object> Students { get; set; }

        public ObservableCollection<object> GroupStudents { get; set; }
        [Reactive] [CanBeNull] public DepartmentEntity SelectedDepartment { get; set; }
        [Reactive] public bool DepartmentSelectionAvailable { get; set; } = true;
        [Reactive] [NotNull] public GroupEntity Group { get; set; } = new GroupEntity();
        [Reactive] [CanBeNull] public StudentDropdownItem SelectedChiefStudent { get; set; }

        [Reactive] public DateTime ExpirationDate { get; set; }
        [Reactive] public bool IsGroupActive { get; set; }
        [Reactive] [CanBeNull] public string GroupName { get; set; }
        [Reactive] public bool IsValid { get; set; } = true;

        public ObservableCollection<object> SelectedStudents { get; }

        public ObservableCollection<object> SelectedGroupStudents { get; }

        public ICommand AddStudentsToGroupHandler { get; set; }
        public ICommand RemoveStudentsFromGroupHandler { get; set; }
        public ICommand SaveGroupHandler { get; set; }

        private void AddStudentsToGroup()
        {
            var selectedGroupStudents = this.SelectedStudents.ToList();
            foreach (var selectedGroupStudent in selectedGroupStudents)
            {
                this.GroupStudents.Add(selectedGroupStudent);
                this.Students.Remove(selectedGroupStudent);
            }
        }

        private void RemoveStudentsFromGroup()
        {
            var selectedGroupStudents = this.SelectedGroupStudents.ToList();
            foreach (var selectedGroupStudent in selectedGroupStudents)
            {
                this.GroupStudents.Remove(selectedGroupStudent);
                this.Students.Add(selectedGroupStudent);
            }
        }


        protected override string GetLocalizationKey()
        {
            return "group.form";
        }

        private async Task Save()
        {
            this.Group.Students = this.GroupStudents
                .Cast<StudentViewModel>()
                .Select(item => item.Student)
                .ToList();
            if (this.Group.Id == 0)
            {
                _db.Groups.Add(this.Group);
            }
            else
            {
                var group = _db.Groups.Find(this.Group.Id);
                group?.Apply(this.Group);
            }

            await _db.SaveChangesAsync();
            _token.Deactivate();
        }
    }

    public class StudentDropdownItem
    {
        public StudentDropdownItem(StudentEntity student)
        {
            this.Student = student;
            this.Name = student.LastName
                        + " "
                        + student.FirstName
                        + " "
                        + student.SecondName;
        }

        public string Name { get; }
        public StudentEntity Student { get; }
    }

    public class StudentViewModel : StudentDropdownItem
    {
        public StudentViewModel(StudentEntity student) : base(student)
        {
            this.Groups = string.Join
            (
                ", ",
                student.Groups.Select
                (
                    groupModel => groupModel.Name
                )
            );
        }

        public string Groups { get; }
    }
}

using System.Linq;
using Model.Models;
using TeacherAssistant.Dao.ViewModels;

namespace TeacherAssistant.Pages.StudentTablePage.ViewModels {
    public class StudentViewModel : IStudentViewModel {
        public string GroupsText { get; set; }
        public StudentEntity Student { get; set; }

        public StudentViewModel(StudentEntity student) {
            this.Student = student;
            this.GroupsText = string.Join(", ", student.Groups.Select(groupModel => groupModel.Name));
        }
    }
}
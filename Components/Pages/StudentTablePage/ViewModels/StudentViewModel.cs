using System.Linq;
using TeacherAssistant.Helpers.ViewModels;
using TeacherAssistant.Models;

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
using System.Linq;
using Model.Models;

namespace TeacherAssistant.StudentTable {
    public partial class StudentTableModel {
        public class StudentViewModel {
            public string GroupsText { get; set; }
            public StudentModel Model { get; }

            public StudentViewModel(StudentModel model) {
                this.Model = model;
                this.GroupsText = string.Join(", ", model.Groups.Select(groupModel => groupModel.Name));
            }
        }
    }
}
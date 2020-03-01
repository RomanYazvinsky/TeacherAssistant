using TeacherAssistant.Models;

namespace TeacherAssistant.StudentForm {
    public class ChoseGroupModel {
        public ChoseGroupModel(GroupEntity group) {
            this.Group = group;
        }

        public GroupEntity Group { get; }
        public bool IsPraepostor { get; set; }

        public bool IsPraepostorAlreadySet { get; set; }
    }
}
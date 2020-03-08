using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models {
    public enum LessonType : long {
        Unknown = 0,
        Lecture = 1,
        Practice = 2,
        Laboratory = 3,
        Attestation = 4,
        Exam = 5
    }

    [Table("LESSON_TYPE")]
    public class LessonTypeEntity : Entity {
        [Column("name")] public string Name { get; set; }
    }
}

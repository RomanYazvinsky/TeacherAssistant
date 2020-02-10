using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models {
    public enum LessonType : long {
        Unknown = 0,
        Lecture = 1,
        Practice = 2,
        Laboratory = 3,
        Attestation = 4,
        Exam = 5
    }

    [Table("LESSON_TYPE")]
    public class LessonTypeEntity {
        private string _name;
        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")] public string Name { get; set; }
    }
}

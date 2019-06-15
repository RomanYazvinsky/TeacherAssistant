using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    public enum LessonType: long
    {
        Lecture = 1, Practice = 2, Laboratory = 3
    }

    [Table("LESSON_TYPE")]
    public class LessonTypeModel
    {
        [Key]
        public long id { get; set; }

        public string name { get; set; }
    }
}
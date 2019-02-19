using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("STUDENT_LESSON")]
    public class StudentLessonModel
    {
        [Key]
        public long id { get; set; }

        [ForeignKey("student_id")]
        public virtual StudentModel Student { get; set; }
        [ForeignKey("lesson_id")]
        public virtual LessonModel Lesson { get; set; }

        public long? student_id { get; set; }
        public long? lesson_id { get; set; }
        public long? registered { get; set; }

        public string registration_time { get; set; }

        public string registration_type { get; set; }

        public string mark { get; set; }

        public string mark_time { get; set; }
    }
}
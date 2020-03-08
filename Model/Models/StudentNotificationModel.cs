using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models
{
  //  [Table("STUDENT_NOTIFICATION")]
    public class StudentNotificationModel: Entity
    {

        [ForeignKey("student_id")]
        public virtual StudentEntity student { get; set; }

        public long student_id { get; set; }
        public long active { get; set; }

        public String create_date { get; set; }

        public String description { get; set; }
    }
}
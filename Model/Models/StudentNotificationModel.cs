using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
  //  [Table("STUDENT_NOTIFICATION")]
    public class StudentNotificationModel
    {
        [Key]
        public long id { get; set; }

        [ForeignKey("student_id")]
        public virtual StudentEntity student { get; set; }

        public long student_id { get; set; }
        public long active { get; set; }

        public String create_date { get; set; }

        public String description { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("STUDENT_GROUP")]
    public class StudentGroupModel
    {
        [Key]
        public long id { get; set; }

        [ForeignKey("student_id")]
        public StudentModel Student { get; set; }

        [ForeignKey("group_id")]
        public GroupModel Group { get; set; }

        [ForeignKey("praepostor")]
        public StudentModel Praepostor { get; set; }

        public long? student_id { get; set; }
        public long? group_id { get; set; }
        public long? praepostor { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherAssistant.Models
{
  // [Table("STUDENT_GROUP")]
    public class StudentGroupModel
    {
      //  [Key]
        public long id { get; set; }

        public virtual StudentEntity Student { get; set; }

        public virtual GroupEntity Group { get; set; }
        
        [Key, Column("student_id")]
        public long? student_id { get; set; }
        [Key, Column("group_id")]
        public long? group_id { get; set; }
        public long? praepostor { get; set; }
    }
}
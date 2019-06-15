using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("GROUP")]
    public class GroupModel
    {
        public GroupModel()
        {
            Students = new HashSet<StudentModel>();
        }

        [Key]
        public long id { get; set; }

        public string name { get; set; }

        [ForeignKey("department_id")]
        public DepartmentModel Department { get; set; }

        [ForeignKey("type_id")]
        public GroupTypeModel GroupType { get; set; }
        [ForeignKey("praepostor_id")]
        public StudentModel Praepostor { get; set; }
        public byte[] image { get; set; }
        public virtual ICollection<StudentModel> Students { get; set; }
        public virtual ICollection<StreamModel> Streams { get; set; }

        public Int64? department_id { get; set; }
        public Int64? type_id { get; set; }
        public Int64? active { get; set; }

        public string expiration_date { get; set; }

        public Int64? praepostor_id { get; set; }
    }
}
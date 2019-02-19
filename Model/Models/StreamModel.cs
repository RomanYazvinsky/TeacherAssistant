using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("STREAM")]
    public class StreamModel
    {
        [Key]
        public long id { get; set; }

        public string name { get; set; }

        public byte[] image { get; set; }

        public string description { get; set; }

        public string create_date { get; set; }

        [ForeignKey("lecturer_id")]
        public LecturerModel Lecturer { get; set; }

        [ForeignKey("discipline_id")]
        public DisciplineModel Discipline { get; set; }

        [ForeignKey("department_id")]
        public DepartmentModel Department { get; set; }

        public Int64? lecturer_id { get; set; }
        public Int64? discipline_id { get; set; }
        public Int64? department_id { get; set; }
        public int course { get; set; }

        public int active { get; set; }

        public string expiration_date { get; set; }

        public Int16? lecture_count { get; set; }

        public Int16? practical_count { get; set; }

        public Int16? lab_count { get; set; }
    }
}
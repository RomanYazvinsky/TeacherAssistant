using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("NOTE")]
    public class NoteModel
    {
        [Key]
        public long id { get; set; }

        [ForeignKey("lecturer_id")]
        public LecturerModel Lecturer { get; set; }

        public string type { get; set; }

        public int entity_id { get; set; }
        public long lecturer_id { get; set; }

        public string description { get; set; }

        public string create_date { get; set; }
    }
}
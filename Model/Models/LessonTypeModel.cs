using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("LESSON_TYPE")]
    public class LessonTypeModel
    {
        [Key]
        public long id { get; set; }

        public string name { get; set; }
    }
}
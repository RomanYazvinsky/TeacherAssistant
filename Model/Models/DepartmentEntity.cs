using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
    [Table("DEPARTMENT")]
    public class DepartmentEntity
    {
        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("abbreviation")]
        public string Abbreviation { get; set; }
    }
}

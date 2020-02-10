using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("DISCIPLINE")]
    public class DisciplineEntity
    {
        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("create_date")] public string _CreationDate { get; set; }

        [Column("active")] public long? _IsActive { get; set; }

        [Column("expiration_date")] public string _ExpirationDate { get; set; }
    }
}

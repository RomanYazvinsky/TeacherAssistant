using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("GROUP_TYPE")]
    public class GroupTypeModel
    {
        private string _name;

        [Key, Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}

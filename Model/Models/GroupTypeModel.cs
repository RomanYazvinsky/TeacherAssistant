using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("GROUP_TYPE")]
    public class GroupTypeModel
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }
        
        public string name { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model
{
    [Table("DEPARTMENT")]
    public class DepartmentModel
    {
        [Key]
        public long id { get; set; }
        public string name { get; set; }

        public string abbreviation { get; set; }
    }
}
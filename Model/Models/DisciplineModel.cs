using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    [Table("DISCIPLINE")]
    public class DisciplineModel
    {
        [Key]
        public long id { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public string create_date { get; set; }

        public Int64? active { get; set; }

        public string expiration_date { get; set; }
    }
}

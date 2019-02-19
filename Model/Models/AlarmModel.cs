using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    [Table("ALARM")]
    public class AlarmModel
    {
        [Key]
        public long id { get; set; }

        public Int64? active { get; set; }

        public Int64? time { get; set; }

        public Decimal? volume { get; set; }

        public string sound { get; set; }
    }
}

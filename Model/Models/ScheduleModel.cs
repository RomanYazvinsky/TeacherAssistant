using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    [Table("SCHEDULE")]
    public class ScheduleModel
    {
        [Key]
        public long id { get; set; }

        public DateTime Begin
        {
            get
            {
                return DateTime.Parse(begin);
            }

        }

        public DateTime End
        {
            get
            {
                return DateTime.Parse(end);
            }

        }

        public string begin { get; set; }

        public string end { get; set; }

        public int number { get; set; }
        public long? version_id { get; set; }

        [ForeignKey("version_id")]
        public ScheduleVersionModel Version { get; set; }

        public override string ToString()
        {
            return begin + " - " + end;
        }
    }
}

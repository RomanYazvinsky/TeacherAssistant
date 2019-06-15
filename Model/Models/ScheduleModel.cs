using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("SCHEDULE")]
    public class ScheduleModel : IComparable
    {
        [Key]
        public long id { get; set; }

        public DateTime Begin => DateTime.Parse(begin);

        public DateTime End => DateTime.Parse(end);

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

        public int CompareTo(object obj)
        {
            if (!(obj is ScheduleModel o))
            {
                return 1;
            }
            return Begin.CompareTo(o.Begin);
        }
    }
}

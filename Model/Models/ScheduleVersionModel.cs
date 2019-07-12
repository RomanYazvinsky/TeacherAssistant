using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    // [Table("SCHEDULE_VERSION")]
    public class ScheduleVersionModel
    {
        [Key]
        public long id { get; set; }

        public string start_date { get; set; }

        public string end_date { get; set; }
    }
}
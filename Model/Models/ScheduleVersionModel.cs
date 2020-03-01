using System.ComponentModel.DataAnnotations;

namespace TeacherAssistant.Models
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
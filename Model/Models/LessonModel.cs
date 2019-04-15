using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;

namespace Model.Models
{
    [Table("LESSON")]
    public class LessonModel
    {
        [Key]
        public long id { get; set; }

        public String name { get; set; }

        public String description { get; set; }

        [ForeignKey("stream_id")]
        public StreamModel Stream { get; set; }

        public String create_date { get; set; }

        /// <summary>
        /// Use type_id instead
        /// 1 = Lecture
        /// 2 = Practical
        /// 3 = Lab
        /// </summary>
        [ForeignKey("type_id")]
        public LessonTypeModel LessonType { get; set; }

        [ForeignKey("group_id")]
        public GroupModel Group { get; set; }

        public DateTime Date
        {
            get
            {
                return DateTime.ParseExact(DATE.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

        }
        public string DATE { get; set; }

        [ForeignKey("SCHEDULE_ID")]
        public ScheduleModel Schedule { get; set; }

        public Int64? stream_id { get; set; }
        public long? type_id { get; set; }
        public long? group_id { get; set; }
        public long? SCHEDULE_ID { get; set; }
        public long? index_number { get; set; }

        [Required]
        [Column("checked")]
        public int Checked { get; set; }
    }
}
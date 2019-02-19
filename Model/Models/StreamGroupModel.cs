using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("STREAM_GROUP")]
    public class StreamGroupModel
    {
        [Key]
        public long id { get; set; }
        public long? stream_id { get; set; }

        public long? group_id { get; set; }

        [ForeignKey("stream_id")]
        public StreamModel Stream { get; set; }

        [ForeignKey("group_id")]
        public GroupModel Group { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
  //  [Table("STREAM_GROUP")]
    public class StreamGroupModel
    {
        [Key]
        public long id { get; set; }
        public long? stream_id { get; set; }

        public long? group_id { get; set; }

        [ForeignKey("stream_id")]
        public StreamEntity Stream { get; set; }

        [ForeignKey("group_id")]
        public GroupEntity Group { get; set; }
    }
}
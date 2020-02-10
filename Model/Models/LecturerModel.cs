using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    // [Table("LECTURER")]
    public class LecturerModel
    {
        [Key] [Column("id")]
        public long Id { get; set; }

        public String card_uid { get; set; }

        public Int64 card_id { get; set; }

        public String first_name { get; set; }

        public String last_name { get; set; }

        public String patronymic { get; set; }

        public String phone { get; set; }

        public String email { get; set; }

        public Byte[] image { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    [Table("STUDENT")]
    public class StudentModel : IComparable
    {
        [Key]
        public long id { get; set; }

        public string card_uid { get; set; }

        public string card_id { get; set; }

        public string first_name { get; set; }

        public string last_name { get; set; }

        public string patronymic { get; set; }

        public string phone { get; set; }

        public string email { get; set; }

        public byte[] image { get; set; }
        public int CompareTo(object obj)
        {
            return String.Compare((obj as StudentModel)?.last_name, last_name, StringComparison.Ordinal);
        }
    }
}
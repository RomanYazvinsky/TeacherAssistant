﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Model.Models
{
    public enum NoteType
    {
        STUDENT_LESSON,
        STUDENT,
        LESSON,
        Unknown
    }

    [Table("NOTE")]
    public class NoteEntity
    {

        [Key] [Column("id")] public long Id { get; set; }
        // [Column("type")] public string _Type { get; set; }

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("create_date")] public string _CreationDate { get; set; }

        [NotMapped]
        public DateTime Date
        {
            get
            {
                var clearDateTime = this._CreationDate.Replace("T", " ");
                DateTime.TryParseExact
                (
                    clearDateTime,
                    new[] {"yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss.ff", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"},
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var result
                );
                return result;
            }
            set
            {
                this._CreationDate = value.ToString("yyyy-MM-dd HH:mm:ss.fff").Replace(" ", "T");
            }
        }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using TeacherAssistant.Dao;

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
    public class NoteEntity: ATrackable<NoteEntity>
    {
        [Key] [Column("id")] public long Id { get; set; }
        // [Column("type")] public string _Type { get; set; }

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("create_date")] public string _CreationDate { get; set; }

        [NotMapped]
        public DateTime CreationDate
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

        public override void Apply(NoteEntity trackable)
        {
            this.Id = trackable.Id;
            this.EntityId = trackable.EntityId;
            this.Description = trackable.Description;
            this._CreationDate = trackable._CreationDate;
        }

        public override NoteEntity Clone()
        {
            var noteEntity = new NoteEntity();
            noteEntity.Apply(this);
            return noteEntity;
        }
    }
}

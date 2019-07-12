using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;

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
    public class NoteModel : INotifyPropertyChanged
    {
        private long? _entityId;
        private string _description;

        [Key] [Column("id")] public long Id { get; set; }
        // [Column("type")] public string _Type { get; set; }

        [Column("entity_id")]
        public long? EntityId
        {
            get => _entityId;
            set
            {
                if (value == _entityId)
                    return;
                _entityId = value;
                OnPropertyChanged();
            }
        }

        [Column("description")]
        public string Description
        {
            get => _description;
            set
            {
                if (value == _description)
                    return;
                _description = value;
                OnPropertyChanged();
            }
        }

        [Column("create_date")] public string _CreationDate { get; set; }

        /*[NotMapped]
        public NoteType Type
        {
            get => Enum.TryParse(this._Type, out NoteType result) ? result : NoteType.Unknown;
            set
            {
                this._Type = value.ToString();
                OnPropertyChanged();
            }
        }*/

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
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
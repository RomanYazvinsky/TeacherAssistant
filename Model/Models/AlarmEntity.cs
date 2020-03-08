using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models
{
    [Table("ALARM")]
    public class AlarmEntity : ATrackable<AlarmEntity>
    {
        private string _discriminator;
        private string _resourceName;

        public AlarmEntity()
        {
        }

        public AlarmEntity(AlarmEntity alarm)
        {
            Apply(alarm);
        }

        [Column("active")] public long? _Active { get; set; }

        [Column("time")]
        public long? _Timer { get; set; }

        [Column("volume")] public decimal? _Volume { get; set; }

        [Column("sound")] [CanBeNull] public byte[] Sound { get; set; } = new byte[0];

        [Column("discriminator")]
        [CanBeNull]
        public string Discriminator
        {
            get => _discriminator;
            set
            {
                if (string.Equals(value, _discriminator)) return;
                _discriminator = value;
            }
        }

        [Column("resource_name")]
        [CanBeNull]
        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (string.Equals(value, _resourceName)) return;
                _resourceName = value;
            }
        }

        [NotMapped]
        public bool IsActive
        {
            get => this._Active > 0;
            set => this._Active = value ? 1 : 0;
        }
        [NotMapped]
        public decimal Volume
        {
            get => this._Volume ?? 0;
            set => this._Volume = value;
        }

        [NotMapped] public string SoundAsUrl => "data:audio/mpeg;base64," + Convert.ToBase64String(this.Sound);

        [NotMapped]
        public TimeSpan? SinceLessonStart
        {
            get => this._Timer == null ? (TimeSpan?) null : TimeSpan.FromMinutes(this._Timer.Value);
            set => this._Timer = value == null ? null : (long?) (long) value.Value.TotalMinutes;
        }


        public sealed override void Apply(AlarmEntity trackable)
        {
        }

        public override AlarmEntity Clone()
        {
            return new AlarmEntity(this);
        }
    }
}

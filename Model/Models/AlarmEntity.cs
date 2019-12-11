using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model
{
    [Table("ALARM")]
    public class AlarmEntity : Trackable<AlarmEntity>, INotifyPropertyChanged
    {
        private long? _timer;
        private string _discriminator;
        private string _resourceName;

        [Key] [Column("id")] public long Id { get; set; }
        [Column("active")] public long? _Active { get; set; }

        [Column("time")]
        public long? Timer
        {
            get => _timer;
            set
            {
                if (value == _timer)
                    return;
                _timer = value;
                OnPropertyChanged();
            }
        }

        [Column("volume")] public decimal? _Volume { get; set; }

        [Column("sound")] public byte[] _Sound { get; set; } = new byte[0];

        [Column("discriminator")]
        public string Discriminator {
            get => _discriminator;
            set {
                if (value == _discriminator) return;
                _discriminator = value;
                OnPropertyChanged();
            }
        }

        [Column("resource_name")]
        public string ResourceName {
            get => _resourceName;
            set {
                if (value == _resourceName) return;
                _resourceName = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public bool IsActive
        {
            get => this._Active > 0;
            set
            {
                this._Active = value ? 1 : 0;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public decimal Volume
        {
            get => this._Volume ?? default;
            set
            {
                this._Volume = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public string SoundAsUrl => "data:audio/mpeg;base64," + Convert.ToBase64String(this._Sound);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Apply(AlarmEntity trackable) {
        }
    }
}
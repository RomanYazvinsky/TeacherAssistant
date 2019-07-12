using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model
{
    [Table("ALARM")]
    public class AlarmModel : Trackable<AlarmModel>, INotifyPropertyChanged
    {
        private long? _timer;

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

        [Column("sound")] public string _Sound { get; set; }

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
        public byte[] Sound
        {
            get => Encoding.Default.GetBytes(this._Sound);
            set
            {
                this._Sound = Encoding.Default.GetString(value);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Apply(AlarmModel trackable) {
        }
    }
}
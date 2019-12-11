using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model.Models {
    [Table("SCHEDULE")]
    public class ScheduleEntity : INotifyPropertyChanged, IComparable {
        private int _orderNumber;

        [Key] [Column("id")] public long Id { get; set; }

        [NotMapped]
        public TimeSpan? Begin {
            get {
                if (TimeSpan.TryParse(this._Begin, out var result)) {
                    return result;
                }

                return null;
            }
        }

        [NotMapped]
        public TimeSpan? End {
            get {
                if (TimeSpan.TryParse(this._End, out var result)) {
                    return result;
                }

                return null;
            }
        }

        [Column("begin")] public string _Begin { get; set; }
        [Column("end")] public string _End { get; set; }

        [Column("number")]
        public int OrderNumber {
            get => _orderNumber;
            set {
                if (value == _orderNumber)
                    return;
                _orderNumber = value;
                OnPropertyChanged();
            }
        }

        public override string ToString() {
            return this._Begin + " - " + this._End;
        }

        public int CompareTo(object obj) {
            if (!(obj is ScheduleEntity o)) {
                return 1;
            }

            return Begin?.CompareTo(o.Begin) ?? -1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
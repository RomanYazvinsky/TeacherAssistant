using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeacherAssistant.Helpers;

namespace TeacherAssistant.Models {
    [Table("NOTIFICATION_SETTING")]
    public sealed class NotificationSettingsModel : TrackableEntity<NotificationSettingsModel> {
        public NotificationSettingsModel() {
        }

        public NotificationSettingsModel(NotificationSettingsModel other) {
            Apply(other);
        }

        [Column("type")]
        public string Type { get; set; }
        [Column("active")]
        public int DbActive { get; set; }
        [Column("data")]
        public string Data { get; set; }
        [Column("volume")]
        public decimal Volume { get; set; }
        [Column("sound")]
        public string DbSound { get; set; }

        public override void Apply(NotificationSettingsModel trackable) {
        }

        public override NotificationSettingsModel Clone() {
            return new NotificationSettingsModel(this);
        }
    }
}
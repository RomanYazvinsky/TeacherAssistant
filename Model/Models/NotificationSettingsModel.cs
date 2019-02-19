using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    [Table("NOTIFICATION_SETTING")]
    public class NotificationSettingsModel
    {
        [Key]
        public long id { get; set; }

        public string type { get; set; }

        public int active { get; set; }

        public string data { get; set; }

        public decimal volume { get; set; }

        public string sound { get; set; }
    }
}

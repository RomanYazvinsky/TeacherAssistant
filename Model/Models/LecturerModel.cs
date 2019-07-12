using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model.Models
{
    // [Table("LECTURER")]
    public class LecturerModel: INotifyPropertyChanged
    {
        [Key] [Column("id")]
        public long Id { get; set; }

        public String card_uid { get; set; }

        public Int64 card_id { get; set; }

        public String first_name { get; set; }

        public String last_name { get; set; }

        public String patronymic { get; set; }

        public String phone { get; set; }

        public String email { get; set; }

        public Byte[] image { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
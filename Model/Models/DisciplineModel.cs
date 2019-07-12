using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model.Models
{
    [Table("DISCIPLINE")]
    public class DisciplineModel : INotifyPropertyChanged
    {
        private string _name;
        private string _description;

        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                    return;
                _name = value;
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

        [Column("active")] public long? _IsActive { get; set; }

        [Column("expiration_date")] public string _ExpirationDate { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
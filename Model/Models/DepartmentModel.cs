using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model
{
    [Table("DEPARTMENT")]
    public class DepartmentModel : INotifyPropertyChanged
    {
        private string _name;
        private string _abbreviation;

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

        [Column("abbreviation")]
        public string Abbreviation
        {
            get => _abbreviation;
            set
            {
                if (value == _abbreviation)
                    return;
                _abbreviation = value;
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
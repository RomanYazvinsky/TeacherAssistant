using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;

namespace Model.Models {
    public enum LessonType : long {
        Unknown = 0,
        Lecture = 1,
        Practice = 2,
        Laboratory = 3,
        Attestation = 4,
        Exam = 5
    }

    [Table("LESSON_TYPE")]
    public class LessonTypeEntity : INotifyPropertyChanged {
        private string _name;
        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name {
            get => _name;
            set {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
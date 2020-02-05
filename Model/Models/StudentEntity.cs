using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;

namespace Model.Models {
    [Table("STUDENT")]
    public class StudentEntity : Trackable<StudentEntity>, INotifyPropertyChanged {
        private string _cardUid;
        private string _email;
        private string _firstName;
        private string _lastName;
        private string _phoneNumber;
        private string _secondName;

        public StudentEntity() {
        }

        public StudentEntity(StudentEntity entity) {
            Apply(entity);
        }

        [Key] [Column("id")] public long Id { get; set; }

        [Column("card_uid")]
        public virtual string CardUid {
            get => _cardUid;
            set {
                if (value == _cardUid) {
                    return;
                }

                _cardUid = value;
                OnPropertyChanged();
            }
        }

        [Column("first_name")]
        public virtual string FirstName {
            get => _firstName;
            set {
                if (value == _firstName) {
                    return;
                }

                _firstName = value;
                OnPropertyChanged();
            }
        }

        [Column("last_name")]
        public virtual string LastName {
            get => _lastName;
            set {
                if (value == _lastName) {
                    return;
                }

                _lastName = value;
                OnPropertyChanged();
            }
        }

        [Column("patronymic")]
        public virtual string SecondName {
            get => _secondName;
            set {
                if (value == _secondName) {
                    return;
                }

                _secondName = value;
                OnPropertyChanged();
            }
        }

        [Column("phone")]
        public virtual string PhoneNumber {
            get => _phoneNumber;
            set {
                if (value == _phoneNumber) {
                    return;
                }

                _phoneNumber = value;
                OnPropertyChanged();
            }
        }

        [Column("email")]
        public virtual string Email {
            get => _email;
            set {
                if (value == _email) {
                    return;
                }

                _email = value;
                OnPropertyChanged();
            }
        }

        public virtual ICollection<GroupEntity> Groups { get; set; } = new List<GroupEntity>();
        public virtual ICollection<StudentLessonEntity> StudentLessons { get; set; } = new List<StudentLessonEntity>();
        public virtual ICollection<StudentNote> Notes { get; set; } = new List<StudentNote>();


        public event PropertyChangedEventHandler PropertyChanged;

        public sealed override void Apply(StudentEntity entity) {
            this.Id = entity.Id;
            this.CardUid = entity.CardUid;
            this.FirstName = entity.FirstName;
            this.LastName = entity.LastName;
            this.SecondName = entity.SecondName;
            this.PhoneNumber = entity.PhoneNumber;
            this.Email = entity.Email;
            this.Groups = entity.Groups;
            this.StudentLessons = entity.StudentLessons;
           // this.Notes = model.Notes;
        }

        public override StudentEntity Clone()
        {
            return new StudentEntity(this);
        }


        public static string CardUidToId(string cardUid) {
            return int.Parse(cardUid, NumberStyles.HexNumber).ToString();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

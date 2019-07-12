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
    public class StudentModel : Trackable<StudentModel>, INotifyPropertyChanged {
        private string _cardUid;
        private string _email;
        private string _firstName;
        private string _lastName;
        private string _phoneNumber;
        private string _secondName;

        public StudentModel() {
        }

        public StudentModel(StudentModel model) {
            Apply(model);
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

        public virtual ICollection<GroupModel> Groups { get; set; } = new List<GroupModel>();
        public virtual ICollection<StudentLessonModel> StudentLessons { get; set; } = new List<StudentLessonModel>();
        public virtual ICollection<StudentNote> Notes { get; set; } = new List<StudentNote>();


        public event PropertyChangedEventHandler PropertyChanged;

        public override void Apply(StudentModel model) {
            this.Id = model.Id;
            this.CardUid = model.CardUid;
            this.FirstName = model.FirstName;
            this.LastName = model.LastName;
            this.SecondName = model.SecondName;
            this.PhoneNumber = model.PhoneNumber;
            this.Email = model.Email;
            this.Groups = model.Groups;
            this.StudentLessons = model.StudentLessons;
           // this.Notes = model.Notes;
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;

namespace Model.Models {
    [Table("STREAM")]
    public class StreamModel : Trackable<StreamModel>, INotifyPropertyChanged {
        private const string ExpirationDateTemplate = "yyyy-MM-dd";
        private const string ExpirationDateTime = "T00:00:00";
        private string _name;
        private string _description;
        private int? _course;

        public StreamModel() {
        }

        public StreamModel(StreamModel model) {
            Apply(model);
        }

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

        [Column("description")]
        public string Description {
            get => _description;
            set {
                if (value == _description)
                    return;
                _description = value;
                OnPropertyChanged();
            }
        }

        [Column("create_date")] public string _CreationDate { get; set; }

        // [ForeignKey("lecturer_id")] public virtual LecturerModel Lecturer { get; set; }
        [Column("discipline_id")] public long? _DisciplineId { get; set; }

        [ForeignKey(nameof(_DisciplineId))] public virtual DisciplineModel _Discipline { get; set; }
        [Column("department_id")] public long? _DepartmentId { get; set; }
        [ForeignKey(nameof(_DepartmentId))] public virtual DepartmentModel _Department { get; set; }

        public virtual ICollection<GroupModel> Groups { get; set; } = new List<GroupModel>();

        [Column("course")]
        public int? Course {
            get => _course;
            set {
                if (value == _course)
                    return;
                _course = value;
                OnPropertyChanged();
            }
        }

        [Column("active")] public int? _Active { get; set; }
        [Column("expiration_date")] public string _ExpirationDate { get; set; }
        [Column("lecture_count")] public short? _LectureCount { get; set; }
        [Column("practical_count")] public short? _PracticalCount { get; set; }
        [Column("lab_count")] public short? _LabCount { get; set; }

        [NotMapped]
        public DisciplineModel Discipline {
            get => this._Discipline;
            set {
                if (Equals(value, this._Discipline)) return;
                this._Discipline = value;
                this._DisciplineId = value == null ? value.Id : 0;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public DepartmentModel Department {
            get => this._Department;
            set {
                if (Equals(value, this._Department)) return;
                this._Department = value;
                this._DepartmentId = value == null ? value.Id : 0;
                OnPropertyChanged();
            }
        }

        public virtual ICollection<LessonModel> StreamLessons { get; set; }

        [NotMapped]
        public int LectureCount {
            get => this._LectureCount ?? 0;
            set => this._LectureCount = (short) value;
        }

        [NotMapped]
        public int PracticalCount {
            get => this._PracticalCount ?? 0;
            set => this._PracticalCount = (short) value;
        }

        [NotMapped]
        public int LabCount {
            get => this._LabCount ?? 0;
            set => this._LabCount = (short) value;
        }

        [NotMapped]
        public bool IsActive {
            get => this._Active > 0;
            set {
                this._Active = value ? 1 : 0;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public DateTime? ExpirationDate {
            get {
                if (this._ExpirationDate == null) {
                    return null;
                }

                return DateTime.TryParseExact
                (
                    this._ExpirationDate.Substring(0, 10),
                    ExpirationDateTemplate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var result
                )
                    ? result
                    : default(DateTime?);
            }
            set {
                if (value == null) {
                    this._ExpirationDate = null;
                }
                else {
                    this._ExpirationDate = value.Value.ToString(ExpirationDateTemplate) + ExpirationDateTime;
                }

                OnPropertyChanged();
            }
        }


        [NotMapped]
        public DateTime? CreationDate {
            get {
                if (this._CreationDate == null) {
                    return null;
                }

                return DateTime.TryParseExact
                (
                    this._CreationDate.Substring(0, 10),
                    ExpirationDateTemplate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var result
                )
                    ? result
                    : default(DateTime?);
            }
            set {
                if (value == null) {
                    this._CreationDate = null;
                }
                else {
                    this._CreationDate = value.Value.ToString(ExpirationDateTemplate) + ExpirationDateTime;
                }

                OnPropertyChanged();
            }
        }
        [NotMapped]
        public List<StudentModel> Students =>
            this.Groups.Aggregate(new List<StudentModel>(), (list, model) => {
                list.AddRange(model.Students);
                return list;
            });


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Apply(StreamModel trackable) {
            this.Id = trackable.Id;
            this.Name = trackable.Name;
            this.LabCount = trackable.LabCount;
            this.PracticalCount = trackable.PracticalCount;
            this.LectureCount = trackable.LectureCount;
            this.StreamLessons = trackable.StreamLessons;
            this.Groups = trackable.Groups;
            this._Department = trackable._Department;
            this._Discipline = trackable._Discipline;
            this.Description = trackable.Description;
            this._CreationDate = trackable._CreationDate;
            this._ExpirationDate = trackable._ExpirationDate;
        }
    }
}
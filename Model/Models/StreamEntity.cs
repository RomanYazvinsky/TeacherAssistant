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
    public class StreamEntity : Trackable<StreamEntity> {
        private const string ExpirationDateTemplate = "yyyy-MM-dd";
        private const string ExpirationDateTime = "T00:00:00";

        public StreamEntity() {
        }

        public StreamEntity(StreamEntity entity) {
            Apply(entity);
        }

        [Key] [Column("id")] public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")] public string Description { get; set; }

        [Column("create_date")] public string _CreationDate { get; set; }

        // [ForeignKey("lecturer_id")] public virtual LecturerModel Lecturer { get; set; }
        [Column("discipline_id")] public long? _DisciplineId { get; set; }

        [ForeignKey(nameof(_DisciplineId))] public virtual DisciplineEntity Discipline { get; set; }
        [Column("department_id")] public long? _DepartmentId { get; set; }
        [ForeignKey(nameof(_DepartmentId))] public virtual DepartmentEntity Department { get; set; }

        public virtual ICollection<GroupEntity> Groups { get; set; } = new List<GroupEntity>();

        [Column("course")]
        public int? Course { get; set; }

        [Column("active")] public int? _Active { get; set; }
        [Column("expiration_date")] public string _ExpirationDate { get; set; }
        [Column("lecture_count")] public short? _LectureCount { get; set; }
        [Column("practical_count")] public short? _PracticalCount { get; set; }
        [Column("lab_count")] public short? _LabCount { get; set; }

        public virtual ICollection<LessonEntity> StreamLessons { get; set; }

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
            }
        }

        [NotMapped]
        public List<StudentEntity> Students =>
            this.Groups.Aggregate(new List<StudentEntity>(), (list, model) => {
                list.AddRange(model.Students);
                return list;
            });


        public int GetLessonCountByType(LessonType type) {
            switch (type) {
                case LessonType.Lecture:
                    return this.LectureCount;
                case LessonType.Practice:
                    return this.PracticalCount;
                case LessonType.Laboratory:
                    return this.LabCount;
                default: return 0;
            }
        }

        public sealed override void Apply(StreamEntity trackable) {
            this.Id = trackable.Id;
            this.Name = trackable.Name;
            this.LabCount = trackable.LabCount;
            this.PracticalCount = trackable.PracticalCount;
            this.LectureCount = trackable.LectureCount;
            this.StreamLessons = trackable.StreamLessons;
            this.Groups = trackable.Groups;
            this.Department = trackable.Department;
            this.Discipline = trackable.Discipline;
            this.Description = trackable.Description;
            this._CreationDate = trackable._CreationDate;
            this._ExpirationDate = trackable._ExpirationDate;
        }

        public override StreamEntity Clone()
        {
            return new StreamEntity(this);
        }
    }
}

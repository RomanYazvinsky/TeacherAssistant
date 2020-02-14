using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using JetBrains.Annotations;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Dao.ViewModels;

namespace Model.Models
{
    [Table("STUDENT_LESSON")]
    public class StudentLessonEntity: ATrackable<StudentLessonEntity>, IStudentViewModel
    {
        public StudentLessonEntity()
        {

        }

        public StudentLessonEntity(StudentLessonEntity entity)
        {
            Apply(entity);
        }
        [Key] [Column("id")] public long Id { get; set; }
        [Column("student_id")]
        public long _StudentId { get; set; }
        [Column("lesson_id")]
        public long _LessonId { get; set; }
        [ForeignKey(nameof(_StudentId))] public virtual StudentEntity Student { get; set; }
        [ForeignKey(nameof(_LessonId))] public virtual LessonEntity Lesson { get; set; }
        [CanBeNull] public virtual ICollection<StudentLessonNote> Notes { get; set; } = new List<StudentLessonNote>();

        // is null when lesson is not started yet
        [Column("registered")] public long? _Registered { get; set; }
        [Column("registration_time")] public string _RegistrationTime { get; set; }

        [Column("registration_type")] public string RegistrationType { get; set; }

        [Column("mark")] [CanBeNull] public string Mark { get; set; }


        [Column("mark_time")] [CanBeNull] public string _MarkTime { get; set; }

        [NotMapped]
        public DateTime? RegistrationTime
        {
            get
            {
                if (this._RegistrationTime == null)
                    return null;
                DateTime.TryParseExact
                (
                    this._RegistrationTime,
                    "HH:mm:ss.fff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date
                );
                return date;
            }
            set => this._RegistrationTime = value?.ToString("HH:mm:ss.fff");
        }

        [NotMapped]
        public bool? IsRegistered
        {
            get => this._Registered > 0;
            set => this._Registered = value.HasValue ? (value.Value ? 1 : 0) : (long?) null;
        }

        [NotMapped] public bool IsLessonMissed => !this._Registered.HasValue || this._Registered.Value == 0;
        public sealed override void Apply(StudentLessonEntity trackable) {
        }

        public override StudentLessonEntity Clone()
        {
            return new StudentLessonEntity(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Runtime.CompilerServices;
using TeacherAssistant.Annotations;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;

namespace Model.Models
{
    [Table("STUDENT_LESSON")]
    public class StudentLessonModel: Trackable<StudentLessonModel>, INotifyPropertyChanged
    {
        private string _mark;

        [Key] [Column("id")] public long Id { get; set; }
        [Column("student_id")]
        public long _StudentId { get; set; }
        [Column("lesson_id")]
        public long _LessonId { get; set; }
        [ForeignKey(nameof(_StudentId))] public virtual StudentModel _Student { get; set; }
        [ForeignKey(nameof(_LessonId))] public virtual LessonModel _Lesson { get; set; }
        public virtual ICollection<StudentLessonNote> Notes { get; set; } = new List<StudentLessonNote>();

        // is null when lesson is not started yet
        [Column("registered")] public long? _Registered { get; set; }
        [Column("registration_time")] public string _RegistrationTime { get; set; }

        [Column("registration_type")] public string RegistrationType { get; set; }

        [Column("mark")] public string Mark
        {
            get => _mark ?? (_mark = "");
            set
            {
                if (value != null && value == _mark)
                    return;
                _mark = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public StudentModel Student
        {
            get => this._Student;
            set
            {
                if (Equals(value, this._Student))
                    return;
                this._Student = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public LessonModel Lesson
        {
            get => this._Lesson;
            set
            {
                if (Equals(value, this._Lesson))
                    return;
                this._Lesson = value;
                OnPropertyChanged();
            }
        }

        [Column("mark_time")] public string _MarkTime { get; set; }

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
            set
            {
                this._RegistrationTime = value?.ToString("HH:mm:ss.fff");
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public bool? IsRegistered
        {
            get => this._Registered > 0;
            set
            {
                this._Registered = value.HasValue ? (value.Value ? 1 : 0) : (long?) null;
                OnPropertyChanged();
            }
        }

        [NotMapped] public bool IsLessonMissed => !this._Registered.HasValue || this._Registered.Value == 0;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void Apply(StudentLessonModel trackable) {
        }
    }
}
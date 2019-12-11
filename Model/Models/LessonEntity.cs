using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;

namespace Model.Models {
    [Table("LESSON")]
    public class LessonEntity : Trackable<LessonEntity> {
        #region Database mapping

        [Key] [Column("id")] public long Id { get; set; }
        [Column("name")] public string Name { get; set; }
        [Column("description")] public string Description { get; set; }
        [Column("create_date")] public string _CreateDate { get; set; }

        [Column("DATE")] public string _Date { get; set; }
        [Column("type_id")] public long? _TypeId { get; set; }
        [Column("index_number")] public long? _Order { get; set; }

        [Column("checked")] public int _Checked { get; set; }

        #endregion


        public LessonEntity() {
        }

        public LessonEntity(LessonEntity entity) {
            Apply(entity);
        }

        #region ORM FK

        [Column("SCHEDULE_ID")] public long? _ScheduleId { get; set; }
        public virtual ICollection<StudentLessonEntity> StudentLessons { get; set; } = new List<StudentLessonEntity>();
        public virtual ICollection<LessonNote> Notes { get; set; } = new List<LessonNote>();

        [ForeignKey(nameof(_ScheduleId))] public virtual ScheduleEntity Schedule { get; set; }
        [Column("group_id")] public long? _GroupId { get; set; }
        [ForeignKey(nameof(_GroupId))] public virtual GroupEntity Group { get; set; }
        [Column("stream_id")] public long? _StreamId { get; set; }

        [ForeignKey(nameof(_StreamId))] public virtual StreamEntity Stream { get; set; }

        #endregion

        #region Helping properties

        [NotMapped]
        public DateTime? Date {
            get {
                if (this._Date == null || this._Date.Length < 10)
                    return null;
                return DateTime.ParseExact
                (
                    this._Date.Substring(0, 10),
                    new[] {"yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"},
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None
                );
            }
            set => this._Date = value?.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T");
        }

        [NotMapped]
        public int Order {
            get => (int) (this._Order ?? 0);
            set => this._Order = value;
        }

        [NotMapped]
        public DateTime? CreationDate {
            get {
                if (this._CreateDate == null) {
                    return null;
                }

                string clearDate = this._CreateDate.Replace("T", " ");
                return DateTime.ParseExact
                (
                    clearDate.Substring(0, 19),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }
            set => this._CreateDate = value?.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "T");
        }

        [NotMapped]
        public LessonType LessonType {
            get => (LessonType) (this._TypeId.HasValue
                ? Enum.GetValues(typeof(LessonType)).GetValue(this._TypeId.Value)
                : LessonType.Unknown);
            set => this._TypeId = (int) value;
        }

        [NotMapped]
        public bool Checked {
            get => this._Checked > 0;
            set => this._Checked = value ? 1 : 0;
        }

        #endregion

        public int GetLessonsCount() {
            return this.Stream?.GetLessonCountByType(this.LessonType) ?? 0;
        }

        public override void Apply(LessonEntity trackable) {
            this.Id = trackable.Id;
            this.Group = trackable.Group;
            this.Checked = trackable.Checked;
            this.CreationDate = trackable.CreationDate;
            this.Date = trackable.Date;
            this.Description = trackable.Description;
            this.LessonType = trackable.LessonType;
            this.StudentLessons = trackable.StudentLessons;
            this.Name = trackable.Name;
            this.Schedule = trackable.Schedule;
            this.Stream = trackable.Stream;
            this._Order = trackable._Order;
        }
    }
}
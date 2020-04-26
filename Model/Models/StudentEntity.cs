using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using JetBrains.Annotations;
using TeacherAssistant.Helpers;
using TeacherAssistant.Models.Notes;

namespace TeacherAssistant.Models {
    [Table("STUDENT")]
    public class StudentEntity : TrackableEntity<StudentEntity> {
        public StudentEntity() {
        }

        public StudentEntity(StudentEntity entity) {
            Apply(entity);
        }

        [Column("card_uid")] public virtual string CardUid { get; set; }

        [Column("first_name")] public virtual string FirstName { get; set; }

        [Column("last_name")] public virtual string LastName { get; set; }

        [Column("patronymic")] public virtual string SecondName { get; set; }

        [Column("phone")] public virtual string PhoneNumber { get; set; }

        [Column("email")] public virtual string Email { get; set; }

        [CanBeNull] public virtual ICollection<GroupEntity> Groups { get; set; } = new List<GroupEntity>();

        [CanBeNull]
        public virtual ICollection<StudentLessonEntity> StudentLessons { get; set; } = new List<StudentLessonEntity>();

        [CanBeNull] public virtual ICollection<StudentNote> Notes { get; set; } = new List<StudentNote>();


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
            this.Notes = entity.Notes;
        }

        public override StudentEntity Clone() {
            return new StudentEntity(this);
        }


        public static string CardUidToId(string cardUid) {
            return int.Parse(cardUid, NumberStyles.HexNumber).ToString();
        }
        public static bool IsCardUidValid([CanBeNull] string cardUid) {
            return !string.IsNullOrWhiteSpace(cardUid)
                   && cardUid.Length > 6
                   && int.TryParse(cardUid, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        }
    }
}
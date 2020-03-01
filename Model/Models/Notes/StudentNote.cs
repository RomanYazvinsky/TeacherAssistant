using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherAssistant.Models.Notes
{
    public class StudentNote: NoteEntity
    {
        [ForeignKey("EntityId")]
        public virtual StudentEntity Student { get; set; }

        public override void Apply(NoteEntity trackable)
        {
            base.Apply(trackable);
            this.Student = (trackable as StudentNote)?.Student;
        }

        public override NoteEntity Clone()
        {
            var note = new StudentNote();
            note.Apply(this);
            return note;
        }
    }
}

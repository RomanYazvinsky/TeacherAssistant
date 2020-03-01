using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherAssistant.Models.Notes
{
    public class StudentLessonNote: NoteEntity
    {
        [ForeignKey("EntityId")]
        public virtual StudentLessonEntity StudentLesson { get; set; }

        public override void Apply(NoteEntity trackable)
        {
            base.Apply(trackable);
            this.StudentLesson = (trackable as StudentLessonNote)?.StudentLesson;
        }

        public override NoteEntity Clone()
        {
            var note = new StudentLessonNote();
            note.Apply(this);
            return note;
        }
    }
}

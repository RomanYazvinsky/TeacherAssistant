using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherAssistant.Models.Notes
{
    public class LessonNote : NoteEntity
    {
        [ForeignKey("EntityId")] public virtual LessonEntity Lesson { get; set; }

        public override void Apply(NoteEntity trackable)
        {
            base.Apply(trackable);
            this.Lesson = (trackable as LessonNote)?.Lesson;
        }

        public override NoteEntity Clone()
        {
            var note = new LessonNote();
            note.Apply(this);
            return note;
        }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    public class StudentLessonNote: NoteEntity
    {
        [ForeignKey("EntityId")]
        public virtual StudentLessonEntity StudentLesson { get; set; }
    }
}
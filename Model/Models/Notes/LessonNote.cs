using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    
    public class LessonNote: NoteEntity
    {
        [ForeignKey("EntityId")]
        public virtual LessonEntity Lesson { get; set; }
    }
}
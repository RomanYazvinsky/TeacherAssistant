using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    
    public class LessonNote: NoteModel
    {
        [ForeignKey("EntityId")]
        public virtual LessonModel Lesson { get; set; }
    }
}
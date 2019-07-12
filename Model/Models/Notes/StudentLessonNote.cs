using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    public class StudentLessonNote: NoteModel
    {
        [ForeignKey("EntityId")]
        public virtual StudentLessonModel StudentLesson { get; set; }
    }
}
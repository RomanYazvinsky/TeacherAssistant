using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    public class StudentNote: NoteModel
    {
        [ForeignKey("EntityId")]
        public virtual StudentModel Student { get; set; }
    }
}
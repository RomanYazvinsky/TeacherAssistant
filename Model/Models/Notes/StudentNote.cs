using System.ComponentModel.DataAnnotations.Schema;
using Model.Models;

namespace TeacherAssistant.Dao.Notes
{
    public class StudentNote: NoteEntity
    {
        [ForeignKey("EntityId")]
        public virtual StudentEntity Student { get; set; }
    }
}
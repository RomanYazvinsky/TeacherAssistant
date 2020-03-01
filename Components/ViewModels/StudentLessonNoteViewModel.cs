using TeacherAssistant.Models.Notes;

namespace TeacherAssistant.ViewModels {
    public class StudentLessonNoteViewModel {
        public StudentLessonNote Note { get; set; }

        public string LessonTime { get; set; }

        public StudentLessonNoteViewModel(StudentLessonNote note) {
            this.Note = note;
            this.LessonTime = $"{note.StudentLesson.Lesson.Date:dd.MM.yyyy} {note.StudentLesson.Lesson.Schedule.Begin:hh\\:mm}";
        }
    }
}

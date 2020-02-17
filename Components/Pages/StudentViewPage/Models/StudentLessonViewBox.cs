using System.Windows.Input;
using System.Windows.Media;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Database;

namespace TeacherAssistant.StudentViewPage {
    public class StudentLessonViewBox {
        private readonly StudentViewPageModel _model;
        private readonly LocalDbContext _context;

        public StudentLessonViewBox(StudentLessonEntity studentLesson, StudentViewPageModel model, LocalDbContext context) {
            _model = model;
            _context = context;
            this.StudentLesson = studentLesson;
            this.LessonTypeName = LocalizationContainer.Localization[$"common.lesson.type.{studentLesson.Lesson.LessonType}"];

            this.ToggleRegistrationHandler =
                ReactiveCommand.Create(() => model.ToggleRegistration(this));
            this.OpenLessonHandler =
                ReactiveCommand.Create(() => model.OpenLesson(StudentLesson.Lesson));
            this.ShowLessonNotesHandler =
                ReactiveCommand.Create(() => model.ShowStudentLessonNotes(StudentLesson));
            this.IsLessonNotesVisible = (this.StudentLesson.Lesson.Notes?.Count ?? 0) > 0;
            this.IsStudentNotesVisible = (this.StudentLesson.Notes?.Count ?? 0) > 0;
            this.LessonTime = $"{studentLesson.Lesson.Schedule?.Begin:hh\\:mm}-{studentLesson.Lesson.Schedule?.End:hh\\:mm}";
            switch (studentLesson.Lesson.LessonType) {
                case LessonType.Lecture: {
                    this.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 0));
                    break;
                }

                case LessonType.Practice: {
                    this.Background = new SolidColorBrush(Color.FromArgb(20, 50, 50, 255));
                    break;
                }

                case LessonType.Laboratory: {
                    this.Background = new SolidColorBrush(Color.FromArgb(20, 50, 255, 50));
                    break;
                }
            }

            this.Border = !studentLesson.IsRegistered.HasValue || studentLesson.IsRegistered == false
                ? new SolidColorBrush(Color.FromRgb(255, 0, 0))
                : new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
        }

        public ICommand ToggleRegistrationHandler { get; }
        public ICommand OpenLessonHandler { get; }
        public ICommand ShowLessonNotesHandler { get; }

        public StudentLessonEntity StudentLesson { get; }
        public string LessonTypeName { get; }
        public Brush Background { get; }
        public Brush Border { get; }

        public string LessonTime { get; set; }

        public bool IsLessonNotesVisible { get; set; }
        public bool IsStudentNotesVisible { get; set; }

        public string LessonMark {
            get => this.StudentLesson.Mark;
            set {
                this.StudentLesson.Mark = string.IsNullOrWhiteSpace(value) ? null : value;
                _context.ThrottleSave();
                _model.UpdateLessonMark();
            }
        }
    }
}

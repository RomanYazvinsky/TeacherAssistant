using System.Windows.Media;
using Model.Models;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;

namespace TeacherAssistant.StudentViewPage {
    public class StudentLessonViewBox {
        private readonly StudentViewPageModel _model;

        public StudentLessonViewBox(StudentLessonEntity studentLesson, StudentViewPageModel model) {
            _model = model;
            this.StudentLesson = studentLesson;
            this.LessonTypeName = AbstractModel.Localization[$"common.lesson.type.{studentLesson.Lesson.LessonType}"];

            this.ToggleRegistrationHandler =
                new CommandHandler<object>(box => model.ToggleRegistration(this));
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

        public CommandHandler<object> ToggleRegistrationHandler { get; }

        public StudentLessonEntity StudentLesson { get; }
        public string LessonTypeName { get; }
        public Brush Background { get; }
        public Brush Border { get; }

        public string LessonMark {
            get => this.StudentLesson.Mark;
            set {
                this.StudentLesson.Mark = string.IsNullOrWhiteSpace(value) ? null : value;
                LocalDbContext.Instance.ThrottleSave();
                _model.UpdateLessonMark();
            }
        }
    }
}
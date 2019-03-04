using System.Data.Entity;
using System.Linq;
using Dao;
using Model.Models;

namespace TeacherAssistant.RegistrationPage
{
    public class RegisteredStudentModel
    {
        public StudentLessonModel StudentLessonModel { get; set; }
        // public DateTime RegistrationTime { get; set; }
        public string MissedLessons { get; set; }


        public RegisteredStudentModel(StudentLessonModel model)
        {
            StudentLessonModel = model;
            // RegistrationTime
            var prevLessons = from lessonModel in GeneralDbContext.GetInstance().StudentLessonModels.Include(studentLessonModel => studentLessonModel.Lesson)
                              where lessonModel.student_id.Equals(model.student_id)
                              select lessonModel;
            var lecLessons = from lessonModel in prevLessons where lessonModel.Lesson.type_id == 1 select lessonModel;
            var practLessons = from lessonModel in prevLessons where lessonModel.Lesson.type_id == 2 select lessonModel;
            var labLessons = from lessonModel in prevLessons where lessonModel.Lesson.type_id == 3 select lessonModel;
            MissedLessons = lecLessons.Count() + "/" + practLessons.Count() + "/" + labLessons.Count();
        }
    }
}
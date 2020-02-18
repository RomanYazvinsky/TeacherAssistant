using System;
using Model.Models;
using TeacherAssistant.Services;

namespace TeacherAssistant.Modules.MainModule
{
    public class LessonInterval : IInterval
    {
        public LessonInterval(LessonEntity lesson)
        {
            Lesson = lesson;
            this.StartDateTime = (lesson.Date ?? default) + lesson.Schedule?.Begin ?? default;
            this.EndDateTime = (lesson.Date ?? default) + lesson.Schedule?.End ?? default;
        }

        public LessonEntity Lesson { get; }
        public DateTime StartDateTime { get; }
        public DateTime EndDateTime { get; }
    }
}

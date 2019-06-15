using System.Collections;
using Model.Models;

namespace TeacherAssistant.ComponentsImpl
{
    public class LessonDefaultComparerDesc : IComparer
    {
        public int Compare(object x, object y)
        {
            var lesson1 = (LessonModel) x;
            var lesson2 = (LessonModel) y;
            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (lesson1.DATE == null)
            {
                return -1;
            }

            if (lesson2.DATE == null)
            {
                return 1;
            }

            return lesson1.Date != lesson2.Date ? lesson2.Date.CompareTo(lesson1.Date) : lesson1.Schedule?.Begin.CompareTo(lesson2.Schedule?.Begin) ?? 1;
        }
    }
}
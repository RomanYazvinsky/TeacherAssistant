using System;
using DynamicData.Kernel;
using TeacherAssistant.Models;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage.Utils {
    public static class LessonComparator {
        public static readonly Comparison<LessonEntity> Comparator = (lesson1, lesson2) => {
            if (lesson1.LessonType == LessonType.Attestation
                && lesson2.LessonType == LessonType.Attestation) {
                return lesson2.Date?.CompareTo(lesson1.Date.ValueOr(DateTime.MinValue)) ?? -1;
            }

            if (lesson1.LessonType == LessonType.Attestation
                && lesson2.LessonType == LessonType.Exam) {
                return 1;
            }

            if (lesson1.LessonType == LessonType.Exam
                && lesson2.LessonType == LessonType.Attestation) {
                return -1;
            }

            if (lesson1.LessonType == LessonType.Attestation) {
                return -1;
            }

            if (lesson1.LessonType == LessonType.Exam) {
                return -1;
            }

            if (lesson2.LessonType == LessonType.Attestation) {
                return 1;
            }

            if (lesson2.LessonType == LessonType.Exam) {
                return 1;
            }

            return lesson2.Date?.CompareTo(lesson1.Date.ValueOr(DateTime.MinValue)) ?? -1;
        };
    }
}
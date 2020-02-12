using System.Collections.Generic;
using System.Linq;
using Grace.DependencyInjection;
using Model.Models;
using TeacherAssistant.Components;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonViewModel : ViewModelBase {
        private int _missedLessons = 0;

        private Dictionary<string, StudentLessonMarkModel> _lessonToLessonMark =
            new Dictionary<string, StudentLessonMarkModel>();

        private string _fullName;

        public string FullName {
            get => _fullName;
            set {
                if (value == _fullName) return;
                _fullName = value;
                OnPropertyChanged();
            }
        }

        public int MissedLessons {
            get => _missedLessons;
            set {
                if (value == _missedLessons) return;
                _missedLessons = value;
                OnPropertyChanged();
            }
        }
        public IExportLocatorScope ServiceLocator { get; }

        public Dictionary<string, StudentLessonMarkModel> LessonToLessonMark {
            get => _lessonToLessonMark;
            set {
                if (Equals(value, _lessonToLessonMark)) return;
                _lessonToLessonMark = value;
                OnPropertyChanged();
            }
        }

        public StudentEntity Model { get; set; }

        public StudentLessonViewModel(StudentEntity student,
            Dictionary<string, LessonEntity> lessonModels,
            IExportLocatorScope serviceLocator,
            IPageHost host,
            LocalDbContext context) {
            this.Model = student;
            ServiceLocator = serviceLocator;
            this.FullName = student.LastName + " " + student.FirstName;
            foreach (var keyValuePair in lessonModels) {
                var studentLessonModel =
                    keyValuePair.Value.StudentLessons?.FirstOrDefault(model => model._StudentId == student.Id) ??
                    new StudentLessonEntity {
                        _LessonId = keyValuePair.Value.Id,
                        Lesson = keyValuePair.Value,
                        Student = student,
                        _StudentId = student.Id,
                        IsRegistered = false,
                        Mark = ""
                    };
                if (studentLessonModel.Id == 0) {
                    context.StudentLessons.Add(studentLessonModel);
                    context.ThrottleSave();
                }

                this.LessonToLessonMark.Add(keyValuePair.Key, new StudentLessonMarkModel(studentLessonModel, context, host));
            }

            this.MissedLessons =
                this.LessonToLessonMark.Values.Aggregate(0, (i, model) => model.StudentLesson.IsLessonMissed ? i + 1 : i);
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Containers.Annotations;
using Model.Models;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonView : INotifyPropertyChanged {
        private int _missedLessons = 0;

        private Dictionary<string, StudentLessonMarkModel> _lessonToLessonMark =
            new Dictionary<string, StudentLessonMarkModel>();

        public string FullName { get; set; }

        public int MissedLessons {
            get => _missedLessons;
            set {
                if (value == _missedLessons) return;
                _missedLessons = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, StudentLessonMarkModel> LessonToLessonMark {
            get => _lessonToLessonMark;
            set {
                if (Equals(value, _lessonToLessonMark)) return;
                _lessonToLessonMark = value;
                OnPropertyChanged();
            }
        }

        public StudentEntity Model { get; set; }

        public StudentLessonView(StudentEntity student, Dictionary<string, LessonEntity> lessonModels) {
            this.Model = student;
            this.FullName = student.LastName + " " + student.FirstName;
            foreach (var keyValuePair in lessonModels) {
                var studentLessonModel =
                    keyValuePair.Value.StudentLessons.FirstOrDefault(model => model._StudentId == student.Id) ??
                    new StudentLessonEntity {
                        Lesson = keyValuePair.Value,
                        Student = student,
                        IsRegistered = false,
                        Mark = ""
                    };
                if (studentLessonModel.Id == 0) {
                    LocalDbContext.Instance.StudentLessons.Add(studentLessonModel);
                    LocalDbContext.Instance.ThrottleSave();
                }

                this.LessonToLessonMark.Add(keyValuePair.Key, new StudentLessonMarkModel(studentLessonModel));
            }

            this.MissedLessons =
                this.LessonToLessonMark.Values.Aggregate(0, (i, model) => model.Entity.IsLessonMissed ? i + 1 : i);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
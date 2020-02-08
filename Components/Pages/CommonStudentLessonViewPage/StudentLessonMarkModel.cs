using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using JetBrains.Annotations;
using Model.Models;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonMarkModel : INotifyPropertyChanged {
        public StudentLessonEntity Entity { get; set; }

        public StudentLessonMarkModel(StudentLessonEntity model) {
            this.Entity = model;
            this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
        }

        public string Mark {
            get => this.Entity.Mark;
            set {
                if (this.Entity.Mark.Equals(value)) return;
                this.Entity.Mark = value;
                LocalDbContext.Instance.ThrottleSave();
                OnPropertyChanged();
            }
        }

        public Brush Color { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.Dao;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonMarkModel : ViewModelBase {
        private readonly LocalDbContext _context;
        private Brush _color;
        private StudentLessonEntity _studentLesson;

        public StudentLessonEntity StudentLesson {
            get => _studentLesson;
            set {
                if (Equals(value, _studentLesson)) return;
                _studentLesson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Mark));
            }
        }

        public StudentLessonMarkModel(StudentLessonEntity model, LocalDbContext context) {
            _context = context;
            this.StudentLesson = model;
            this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
            this.ToggleRegistrationHandler = ReactiveCommand.Create(() => {
                model.IsRegistered = model.IsLessonMissed;
                this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
                context.ThrottleSave();
            });
        }

        public string Mark {
            get => this.StudentLesson.Mark;
            set {
                if (this.StudentLesson.Mark.Equals(value)) return;
                this.StudentLesson.Mark = value;
                _context.ThrottleSave();
                OnPropertyChanged();
            }
        }

        public Brush Color {
            get => _color;
            set {
                if (Equals(value, _color)) return;
                _color = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleRegistrationHandler { get; set; }
    }
}

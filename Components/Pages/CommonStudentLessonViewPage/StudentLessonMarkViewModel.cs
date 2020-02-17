using System;
using System.Windows.Input;
using System.Windows.Media;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.Components;
using TeacherAssistant.Dao;
using TeacherAssistant.Database;
using TeacherAssistant.RegistrationPage;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonMarkViewModel : ViewModelBase {
        private readonly LocalDbContext _context;
        private Brush _color;
        private StudentLessonEntity _studentLesson;

        public StudentLessonEntity StudentLesson {
            get => _studentLesson;
            set {
                if (Equals(value, _studentLesson)) return;
                _studentLesson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(this.Mark));
            }
        }

        public StudentLessonMarkViewModel(StudentLessonEntity model, LocalDbContext context, IPageHost pageHost) {
            _context = context;
            this.StudentLesson = model;
            this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
            this.ToggleRegistrationHandler = ReactiveCommand.Create(() => {
                model.IsRegistered = model.IsLessonMissed;
                model.RegistrationTime = model.IsLessonMissed ? (DateTime?) null : DateTime.Now;
                this.Color = model.IsLessonMissed ? Brushes.LightPink : Brushes.White;
                context.ThrottleSave();
            });
            this.OpenRegistrationHandler = ReactiveCommand.Create(() => {
                pageHost.AddPageAsync(new RegistrationPageToken("Регистрация", model.Lesson));
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
        public ICommand OpenRegistrationHandler { get; set; }
    }
}

using System;
using System.Windows.Input;
using System.Windows.Media;
using ReactiveUI;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.RegistrationPage;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonCellViewModel : ViewModelBase {
        private readonly LocalDbContext _context;
        private Brush _color;
        private StudentLessonEntity _studentLesson;


        public StudentLessonCellViewModel(StudentLessonEntity studentLesson, LocalDbContext context, IPageHost pageHost) {
            _context = context;
            this.StudentLesson = studentLesson;
            this.Color = studentLesson.IsLessonMissed ? Brushes.LightPink : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
            this.ToggleRegistrationHandler = ReactiveCommand.Create(() => {
                studentLesson.IsRegistered = studentLesson.IsLessonMissed;
                studentLesson.RegistrationTime = studentLesson.IsLessonMissed ? default : DateTime.Now;
                this.Color = studentLesson.IsLessonMissed ? Brushes.LightPink : Brushes.White;
                context.ThrottleSave();
            });
            this.OpenRegistrationHandler = ReactiveCommand.Create(() => {
                pageHost.AddPageAsync(new RegistrationPageToken("Регистрация", studentLesson.Lesson));
            });
        }

        public string Mark {
            get => this.StudentLesson.Mark;
            set {
                if (this.StudentLesson.Mark?.Equals(value) ?? false) return;
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

        public StudentLessonEntity StudentLesson {
            get => _studentLesson;
            set {
                if (Equals(value, _studentLesson)) return;
                _studentLesson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(this.Mark));
            }
        }
        public ICommand ToggleRegistrationHandler { get; set; }
        public ICommand OpenRegistrationHandler { get; set; }
    }
}
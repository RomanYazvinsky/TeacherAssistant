using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using ReactiveUI;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.PageHostProviders;
using TeacherAssistant.Pages.RegistrationPage;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonCellViewModel : ViewModelBase {
        private readonly LocalDbContext _context;
        private Brush _color;
        private StudentLessonEntity _studentLesson;


        public StudentLessonCellViewModel(
            StudentLessonEntity studentLesson,
            LocalDbContext context,
            IPageHost pageHost,
            WindowPageHost windowPageHost,
            IEnumerable<StudentLessonNote> studentLessonNotes
        ) {
            _context = context;
            this.StudentLesson = studentLesson;
            var noteEntities = studentLessonNotes as StudentLessonNote[] ?? studentLessonNotes.ToArray();
            this.ShowNotesInfo = noteEntities.Any();
            this.Color = studentLesson.IsLessonMissed
                ? Brushes.LightPink
                : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
            this.Color.Freeze();
            this.ToggleRegistrationHandler = ReactiveCommand.Create(() => {
                studentLesson.IsRegistered = studentLesson.IsLessonMissed;
                studentLesson.RegistrationTime = studentLesson.IsLessonMissed ? default : DateTime.Now;
                this.Color = studentLesson.IsLessonMissed ? Brushes.LightPink : Brushes.White;
                this.Color.Freeze();
                context.ThrottleSave();
            });
            this.OpenRegistrationHandler = ReactiveCommand.Create(() => {
                pageHost.AddPageAsync(new RegistrationPageToken("Регистрация", studentLesson.Lesson));
            });
            this.OpenNotesFormHandler = ReactiveCommand.Create(() => {
                windowPageHost.AddPageAsync(new NoteListFormToken(
                    "Заметки",
                    () => new StudentLessonNote {
                        EntityId = studentLesson.Id,
                        StudentLesson = studentLesson
                    }, noteEntities
                ));
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

        public bool ShowNotesInfo { get; }

        public ICommand ToggleRegistrationHandler { get; }
        public ICommand OpenRegistrationHandler { get; }
        public ICommand OpenNotesFormHandler { get; }
    }
}
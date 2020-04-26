using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using TeacherAssistant.Database;
using TeacherAssistant.Forms.NoteForm;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.Pages.RegistrationPage;
using TeacherAssistant.Services.Paging;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonCellViewModel : ViewModelBase {
        private readonly LocalDbContext _context;
        private StudentLessonEntity _studentLesson;
        private bool _isRegistered;


        public StudentLessonCellViewModel(
            LessonEntity lesson,
            StudentEntity student,
            StudentLessonEntity studentLesson,
            LocalDbContext context,
            IComponentHost componentHost,
            WindowComponentHost windowComponentHost,
            IEnumerable<StudentLessonNote> studentLessonNotes
        ) {
            _context = context;
            this.Lesson = lesson;
            this.Student = student;
            this.StudentLesson = studentLesson;
            var noteEntities = studentLessonNotes as StudentLessonNote[] ?? studentLessonNotes.ToArray();
            this.ShowNotesInfo = noteEntities.Any();
            this.IsRegistered = !studentLesson.IsLessonMissed;
            this.ToggleRegistrationHandler = ReactiveCommand.Create(() => {
                studentLesson.IsRegistered = studentLesson.IsLessonMissed;
                studentLesson.RegistrationTime = studentLesson.IsLessonMissed ? default : DateTime.Now;
                this.IsRegistered = studentLesson.IsRegistered.Value;
                context.ThrottleSave();
            });
            this.OpenRegistrationHandler = ReactiveCommand.Create(() => {
                componentHost.AddPageAsync(new RegistrationPageToken("Регистрация", studentLesson.Lesson));
            });
            this.OpenNotesFormHandler = ReactiveCommand.Create(() => {
                windowComponentHost.AddPageAsync(new NoteListFormToken(
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
                if (this.StudentLesson.Mark == value) return;
                this.StudentLesson.Mark = value;
                _context.ThrottleSave();
                OnPropertyChanged();
            }
        }

        public bool IsRegistered {
            get => _isRegistered;
            set {
                if (value == _isRegistered) return;
                _isRegistered = value;
                OnPropertyChanged();
            }
        }

        public LessonEntity Lesson { get; }
        public StudentEntity Student { get; }

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
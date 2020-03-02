using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Grace.DependencyInjection;
using ReactiveUI;
using TeacherAssistant.Core.Utils;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services;
using TeacherAssistant.Services.Paging;
using TeacherAssistant.StudentViewPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonViewModel : ViewModelBase {
        private const double DefaultInvalidSummary = -1;
        private readonly Dictionary<string, LessonEntity> _lessonModels;
        private readonly IPageHost _host;
        private readonly LocalDbContext _context;
        private int _missedLessons = 0;
        private BitmapImage _source;
        private bool _isPopupOpened;

        private Dictionary<string, StudentLessonMarkViewModel> _lessonToLessonMark =
            new Dictionary<string, StudentLessonMarkViewModel>();

        private string _fullName;
        private string _attestationSummary;

        public StudentLessonViewModel(
            StudentEntity student,
            Dictionary<string, LessonEntity> lessonModels,
            IExportLocatorScope serviceLocator,
            IPageHost host,
            LocalDbContext context
        ) {
            _lessonModels = lessonModels;
            _host = host;
            _context = context;
            OpenStudentHandler = ReactiveCommand.Create(OpenStudent);
            OpenImageHandler = ReactiveCommand.Create(LoadImage);
            this.Student = student;
            ServiceLocator = serviceLocator;
            this.FullName = $"{student.LastName} {student.FirstName}";
            Init();
        }

        private void Init() {
            foreach (var keyValuePair in _lessonModels) {
                var studentLessonModel =
                    keyValuePair.Value.StudentLessons?.FirstOrDefault(model => model._StudentId == this.Student.Id) ??
                    new StudentLessonEntity {
                        _LessonId = keyValuePair.Value.Id,
                        Lesson = keyValuePair.Value,
                        Student = this.Student,
                        _StudentId = this.Student.Id,
                        IsRegistered = false,
                        Mark = ""
                    };
                if (studentLessonModel.Id == default) {
                    _context.StudentLessons.Add(studentLessonModel);
                }

                this.LessonToLessonMark.Add(keyValuePair.Key,
                    new StudentLessonMarkViewModel(studentLessonModel, _context, _host));
            }

            _context.ThrottleSave();
            this.MissedLessons = this.LessonToLessonMark
                .Values
                .Select(model => model.StudentLesson.IsLessonMissed ? 1 : 0)
                .Sum();
            CalculateAttestationSummary();
        }

        private void CalculateAttestationSummary() {
            var attestationModels = this.LessonToLessonMark
                .Values
                .Where(model => model.StudentLesson.Lesson.LessonType == LessonType.Attestation)
                .ToList();
            foreach (var model in attestationModels) {
                model.PropertyChanged += (sender, args) => {
                    if (nameof(StudentLessonMarkViewModel.Mark).Equals(args.PropertyName)) {
                        CalculateAttestationSummary(attestationModels);
                    }
                };
            }
            CalculateAttestationSummary(attestationModels);
        }

        private void CalculateAttestationSummary(IEnumerable<StudentLessonMarkViewModel> markViewModels) {
            var attestationSummary = markViewModels
                .Where(model => LessonUtil.IsValueValidMark(model.Mark))
                .Select(model => double.Parse(model.Mark))
                .DefaultIfEmpty(DefaultInvalidSummary)
                .Average();
            this.AttestationSummary = attestationSummary.IsEqualWithTolerance(DefaultInvalidSummary)
                ? string.Empty
                : attestationSummary.ToString(CultureInfo.InvariantCulture);
        }

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

        public Dictionary<string, StudentLessonMarkViewModel> LessonToLessonMark {
            get => _lessonToLessonMark;
            set {
                if (Equals(value, _lessonToLessonMark)) return;
                _lessonToLessonMark = value;
                OnPropertyChanged();
            }
        }


        private async void LoadImage() {
            IsPopupOpened = true;
            var service = ServiceLocator.Locate<PhotoService>();
            var path = await service.DownloadPhoto(StudentEntity.CardUidToId(this.Student.CardUid));
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            Source = service.GetImage(path);
        }

        private void OpenStudent() {
            var tabPageHost = ServiceLocator.Locate<TabPageHost>();
            tabPageHost.AddPageAsync(new StudentViewPageToken("Студент", this.Student));
        }


        public BitmapImage Source {
            get => _source;
            set {
                if (Equals(value, _source)) return;
                _source = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenStudentHandler { get; set; }
        public ICommand OpenImageHandler { get; set; }

        public string AttestationSummary {
            get => _attestationSummary;
            set {
                if (value == _attestationSummary) return;
                _attestationSummary = value;
                OnPropertyChanged();
            }
        }

        public bool IsPopupOpened {
            get => _isPopupOpened;
            set {
                if (value == _isPopupOpened) return;
                _isPopupOpened = value;
                OnPropertyChanged();
            }
        }

        public StudentEntity Student { get; set; }
    }
}
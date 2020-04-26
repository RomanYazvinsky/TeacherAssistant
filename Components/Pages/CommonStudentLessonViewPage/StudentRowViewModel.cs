using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Grace.DependencyInjection;
using ReactiveUI;
using TeacherAssistant.Core.Utils;
using TeacherAssistant.Database;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services;
using TeacherAssistant.Services.Paging;
using TeacherAssistant.StudentViewPage;
using TeacherAssistant.Utils;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentRowViewModel : ViewModelBase {
        private const double DefaultInvalidSummary = -1;
        private readonly Dictionary<long, LessonEntity> _lessonModels;
        private readonly IComponentHost _host;
        private readonly WindowComponentHost _windowComponentHost;
        private readonly LocalDbContext _context;
        private readonly IEnumerable<StudentLessonNote> _notes;
        private string _missedLessonsInfo;
        private BitmapImage _source;
        private bool _isPopupOpened;

        private Dictionary<long, StudentLessonCellViewModel> _lessonToLessonMark =
            new Dictionary<long, StudentLessonCellViewModel>();

        private string _fullName;
        private string _attestationSummary;

        public StudentRowViewModel(
            StudentEntity student,
            Dictionary<long, LessonEntity> lessonModels,
            IExportLocatorScope serviceLocator,
            IComponentHost host,
            WindowComponentHost windowComponentHost,
            LocalDbContext context,
            IEnumerable<StudentLessonNote> notes
        ) {
            _lessonModels = lessonModels;
            _host = host;
            _windowComponentHost = windowComponentHost;
            _context = context;
            _notes = notes;
            this.OpenStudentHandler = ReactiveCommand.Create(OpenStudent);
            this.OpenImageHandler = ReactiveCommand.Create(LoadImage);
            this.Student = student;
            this.ServiceLocator = serviceLocator;
            this.FullName = $"{student.LastName} {student.FirstName}";
            Init();
        }

        private void Init() {
            foreach (var keyValuePair in _lessonModels) {
                var lesson = keyValuePair.Value;
                var studentLessons = lesson.StudentLessons;
                var studentLesson = studentLessons?.FirstOrDefault(model => model._StudentId == this.Student.Id);
                var notes = _notes.Where(note => note.EntityId == studentLesson?.Id);
                var cell = new StudentLessonCellViewModel(lesson, this.Student, studentLesson, _context, _host, _windowComponentHost, notes);
                cell.PropertyChanged += (sender, args) => {
                    if (nameof(StudentLessonCellViewModel.IsRegistered).Equals(args.PropertyName)) {
                        BuildMissedLessonsInfo();
                    }
                };
                this.LessonToLessonMark.Add(keyValuePair.Key, cell);
            }
            BuildMissedLessonsInfo();
            CalculateAttestationSummary();
        }

        private void BuildMissedLessonsInfo() {
            var missedLectures = this.LessonToLessonMark
                .Values
                .Where(model => model.Lesson.LessonType == LessonType.Lecture)
                .Select(model => model.StudentLesson.IsLessonMissed ? 1 : 0)
                .Sum();

            var missedPractices = this.LessonToLessonMark
                .Values
                .Where(model => model.Lesson.LessonType == LessonType.Practice)
                .Select(model => model.StudentLesson.IsLessonMissed ? 1 : 0)
                .Sum();

            var missedLabs = this.LessonToLessonMark
                .Values
                .Where(model => model.Lesson.LessonType == LessonType.Laboratory)
                .Select(model => model.StudentLesson.IsLessonMissed ? 1 : 0)
                .Sum();
            this.MissedLessonsInfo =
                $"{missedLectures + missedPractices + missedLabs} ({missedLectures}/{missedPractices}/{missedLabs})";
        }

        private void CalculateAttestationSummary() {
            var attestationModels = this.LessonToLessonMark
                .Values
                .Where(model => model.Lesson.LessonType == LessonType.Attestation)
                .ToList();
            foreach (var model in attestationModels) {
                model.PropertyChanged += (sender, args) => {
                    if (nameof(StudentLessonCellViewModel.Mark).Equals(args.PropertyName)) {
                        CalculateAttestationSummary(attestationModels);
                    }
                };
            }

            CalculateAttestationSummary(attestationModels);
        }

        private void CalculateAttestationSummary(IEnumerable<StudentLessonCellViewModel> markViewModels) {
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

        public string MissedLessonsInfo {
            get => _missedLessonsInfo;
            set {
                if (value == _missedLessonsInfo) return;
                _missedLessonsInfo = value;
                OnPropertyChanged();
            }
        }

        public IExportLocatorScope ServiceLocator { get; }

        public Dictionary<long, StudentLessonCellViewModel> LessonToLessonMark {
            get => _lessonToLessonMark;
            set {
                if (Equals(value, _lessonToLessonMark)) return;
                _lessonToLessonMark = value;
                OnPropertyChanged();
            }
        }


        private async Task LoadImage() {
            this.IsPopupOpened = true;
            var service = ServiceLocator.Locate<PhotoService>();
            var path = await service.DownloadPhoto(StudentEntity.CardUidToId(this.Student.CardUid));
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            this.Source = service.GetImage(path);
        }

        private void OpenStudent() {
            var tabPageHost = ServiceLocator.Locate<TabComponentHost>();
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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Grace.DependencyInjection;
using Model.Models;
using ReactiveUI;
using TeacherAssistant.Database;
using TeacherAssistant.PageBase;
using TeacherAssistant.Services;
using TeacherAssistant.Services.Paging;
using TeacherAssistant.StudentViewPage;

namespace TeacherAssistant.Pages.CommonStudentLessonViewPage {
    public class StudentLessonViewModel : ViewModelBase {
        private readonly Dictionary<string, LessonEntity> _lessonModels;
        private readonly IPageHost _host;
        private readonly LocalDbContext _context;
        private int _missedLessons = 0;
        private BitmapImage _source;
        private bool _isPopupOpened;

        private Dictionary<string, StudentLessonMarkViewModel> _lessonToLessonMark =
            new Dictionary<string, StudentLessonMarkViewModel>();

        private string _fullName;

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
            this.Model = student;
            ServiceLocator = serviceLocator;
            this.FullName = student.LastName + " " + student.FirstName;
            Init();
        }
        private void Init() {
            foreach (var keyValuePair in _lessonModels) {
                var studentLessonModel =
                    keyValuePair.Value.StudentLessons?.FirstOrDefault(model => model._StudentId == this.Model.Id) ??
                    new StudentLessonEntity {
                        _LessonId = keyValuePair.Value.Id,
                        Lesson = keyValuePair.Value,
                        Student = this.Model,
                        _StudentId = this.Model.Id,
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
            this.MissedLessons =
                this.LessonToLessonMark.Values.Aggregate(0,
                                                         (i, model) => model.StudentLesson.IsLessonMissed ? i + 1 : i);
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
            var path = await service.DownloadPhoto(StudentEntity.CardUidToId(Model.CardUid));
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            Source = service.GetImage(path);
        }

        private void OpenStudent() {
            var tabPageHost = ServiceLocator.Locate<TabPageHost>();
            tabPageHost.AddPageAsync(new StudentViewPageToken("Студент", Model));
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

        public bool IsPopupOpened {
            get => _isPopupOpened;
            set {
                if (value == _isPopupOpened) return;
                _isPopupOpened = value;
                OnPropertyChanged();
            }
        }

        public StudentEntity Model { get; set; }
    }
}

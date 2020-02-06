using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Containers;
using DynamicData;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Pages;
using TeacherAssistant.RegistrationPage;
using TeacherAssistant.StudentForm;
using TeacherAssistant.Utils;

namespace TeacherAssistant.StudentViewPage {
    public class StudentViewPageModel : AbstractModel<StudentViewPageModel> {
        private const string LocalizationKey = "page.student.view";
        private readonly TabPageHost _host;
        private readonly PhotoService _photoService;
        private readonly LocalDbContext _context;

        public StudentViewPageModel(
            StudentViewPageToken token,
            PageControllerReducer reducer,
            TabPageHost host,
            PhotoService photoService,
            LocalDbContext context) {
            _host = host;
            _photoService = photoService;
            _context = context;
            this.AddAttestationButtonConfig = new ButtonConfig {
                Command = new CommandHandler(AddAttestation),
                Text = "+"
            };
            this.AddExamButtonConfig = new ButtonConfig {
                Command = new CommandHandler(AddExam),
                Text = "+"
            };
            this.OpenExternalLesson = new ButtonConfig {
                Command = new CommandHandler(() => {
                    var selectedExternalLesson = this.SelectedExternalLesson;
                    if (selectedExternalLesson == null) {
                        return;
                    }

                    host.AddPageAsync(new RegistrationPageToken("Регистрация", selectedExternalLesson.Lesson));
                })
            };
            this.WhenAnyValue(model => model.Student)
                .Where(LambdaHelper.NotNull)
                .ObserveOnDispatcher(DispatcherPriority.Background)
                .Subscribe
                (
                    student => {
                        UpdateExternalLessons(student);
                        UpdateStudentLessonNotes(student);
                        UpdateStudentNotes(student);
                    }
                );
            this.WhenAnyValue(model => model.SelectedStream).Subscribe(OnSelectedStreamUpdate);
            this.WhenAnyValue(model => model.SelectedGroup).Subscribe(OnSelectedGroupUpdate);
            Initialize(token.Student);
            reducer.Dispatch(new RegisterControlsAction(token, GetControls()));
        }

        private void UpdateExternalLessons(StudentEntity student) {
            this.ExternalLessons.Clear();
            var lessons = GetExternalLessons(student);
            this.ExternalLessons.AddRange(lessons);
            this.ExternalLessonsVisibility = lessons.Count > 0;
            InterpolateLocalization("page.student.view.external.lessons", lessons.Count);
        }

        private void UpdateStudentLessonNotes(StudentEntity student) {
            var studentLessonNotes = _context.StudentLessonNotes
                .Include(note => note.StudentLesson)
                .Where(note => note.StudentLesson._StudentId == student.Id)
                .ToList();
            this.StudentLessonNotes.Clear();
            this.StudentLessonNotes.AddRange(studentLessonNotes);
            InterpolateLocalization("page.student.view.lesson.notes", studentLessonNotes.Count);
            this.IsStudentLessonNotesEnabled = studentLessonNotes.Count > 0;
        }

        private void UpdateStudentNotes(StudentEntity student) {
            var studentNotes = student.Notes.ToList();
            this.StudentNotes.Clear();
            this.StudentNotes.AddRange(studentNotes);
            InterpolateLocalization("page.student.view.student.notes", studentNotes.Count);
            this.IsStudentNotesEnabled = studentNotes.Count > 0;
        }

        private void OnSelectedStreamUpdate(StreamEntity stream) {
            if (stream == null) {
                this.StreamDataVisibility = Visibility.Hidden;
                return;
            }

            this.StreamDataVisibility = Visibility.Visible;
            InterpolateLocalization
            (
                "page.student.view.stream.course",
                stream.Course?.ToString() ?? ""
            );
            InterpolateLocalization
            (
                "page.student.view.discipline.lessons",
                stream.LectureCount,
                stream.PracticalCount,
                stream.LabCount
            );
            var missedLessons = LocalDbContext.Instance.GetStudentMissedLessons(this.Student, stream, DateTime.Now);
            var missedLectures = missedLessons.Count(model => model.Lesson.LessonType == LessonType.Lecture);
            var missedPractices = missedLessons.Count(model => model.Lesson.LessonType == LessonType.Practice);
            var missedLabs = missedLessons.Count(model => model.Lesson.LessonType == LessonType.Laboratory);
            var total = missedLectures + missedPractices + missedLabs;
            this.StudentMissedLessons = total == 0
                ? Localization["Нет пропущенных занятий"]
                : LocalizationContainer.Interpolate
                (
                    "page.student.view.missed.lessons",
                    total,
                    missedLectures,
                    missedPractices,
                    missedLabs
                );
        }

        private async void OnSelectedGroupUpdate(GroupEntity selectedGroup) {
            if (selectedGroup == null) {
                this.StudentGroups.Clear();
                return;
            }

            var groupLessons = await _context.GetGroupLessons(selectedGroup).ToListAsync();
            this.SelectedStream = _context.Streams
                .Include(model => model.Discipline)
                .FirstOrDefault(model => model.Groups.Any(groupModel => groupModel.Id == selectedGroup.Id));
            var lessons = (await _context.StudentLessons
                    .Where(lessonModel => this.Student.Id == lessonModel.Student.Id)
                    .ToListAsync())
                .Where(lessonModel => groupLessons.Any(model => model.Id == lessonModel.Lesson.Id))
                .ToArray(); // to init lazy sequence
            var studentLessonViewBoxes = lessons
                .Where(view => view.Lesson.LessonType < LessonType.Attestation)
                .Select(model => new StudentLessonViewBox(model, this))
                .ToList();
            this.StudentLessons.Clear();
            this.StudentLessons.AddRange(studentLessonViewBoxes);
            var attestations = 0;
            var exams = 0;
            var studentAttestationExamViews = lessons.Where(view => view.Lesson.LessonType == LessonType.Attestation)
                .Select(model => new StudentAttestationExamView(model, this, ++attestations))
                .OrderBy(view => view.StudentLesson.Lesson._Order)
                .ToList();
            this.StudentAttestations.Clear();
            this.StudentAttestations.AddRange(studentAttestationExamViews);
            var attestationExamViews = lessons.Where(view => view.Lesson.LessonType == LessonType.Exam)
                .Select(model => new StudentAttestationExamView(model, this, ++exams))
                .OrderBy(view => view.StudentLesson.Lesson._Order)
                .ToList();
            this.StudentExams.Clear();
            this.StudentExams.AddRange(attestationExamViews);
            this.AddExamButtonConfig.IsEnabled = this.StudentExams.Count < 1;
            UpdateLessonMark();
            UpdateExamMark();
        }

        private async void Initialize(StudentEntity student) {
            this.Student = student;
            this.Groups = string.Join(", ", student.Groups.Select(group => group.Name));
            this.StudentGroups.Clear();
            this.StudentGroups.AddRange(student.Groups);
            this.IsStudentGroupsSelectorEnabled = this.StudentGroups.Count > 1;
            this.SelectedGroup = this.StudentGroups.Count > 0
                ? this.StudentGroups.FirstOrDefault(groupModel => groupModel.IsActive) ?? this.StudentGroups[0]
                : null;
            var path = await _photoService.DownloadPhoto
                    (StudentEntity.CardUidToId(student.CardUid))
                .ConfigureAwait(false);
            var image = _photoService.GetImage(path);
            RunInUiThread(() => { this.StudentPhoto = image; });
        }

        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public string Groups { get; set; }
        [Reactive] public StudentEntity Student { get; set; }
        [Reactive] public StudentLessonEntity SelectedExternalLesson { get; set; }

        public ObservableCollection<StudentLessonEntity> ExternalLessons { get; set; } =
            new ObservableCollection<StudentLessonEntity>();

        [Reactive] public bool ExternalLessonsVisibility { get; set; }

        public ObservableCollection<StudentNote> StudentNotes { get; set; } =
            new ObservableCollection<StudentNote>();

        public ObservableCollection<StudentLessonNote> StudentLessonNotes { get; set; } =
            new ObservableCollection<StudentLessonNote>();

        public ObservableCollection<StudentLessonViewBox> StudentLessons { get; set; } =
            new ObservableCollection<StudentLessonViewBox>();

        public ObservableCollection<StudentAttestationExamView> StudentAttestations { get; set; } =
            new ObservableCollection<StudentAttestationExamView>();

        public ObservableCollection<StudentAttestationExamView> StudentExams { get; set; } =
            new ObservableCollection<StudentAttestationExamView>();

        [Reactive] public bool IsStudentNotesEnabled { get; set; }

        [Reactive] public bool IsStudentLessonNotesEnabled { get; set; }

        [Reactive] public bool IsStudentGroupsSelectorEnabled { get; set; }
        [Reactive] public Visibility StreamDataVisibility { get; set; }

        [Reactive] public GroupEntity SelectedGroup { get; set; }

        [Reactive] public StreamEntity SelectedStream { get; set; }

        public ObservableCollection<GroupEntity> StudentGroups { get; set; } =
            new ObservableCollection<GroupEntity>();

        [Reactive] public string StudentMissedLessons { get; set; } = "";

        [Reactive] public double AverageMark { get; set; }

        public ObservableCollection<MarkStatistics> NumberMarkStatistics { get; set; } =
            new ObservableCollection<MarkStatistics>();

        public ObservableCollection<MarkStatistics> StringMarkStatistics { get; set; } =
            new ObservableCollection<MarkStatistics>();

        [Reactive] public string ResultAttestationMark { get; set; }
        [Reactive] public string ResultMark { get; set; }
        public ButtonConfig AddAttestationButtonConfig { get; set; }
        public ButtonConfig AddExamButtonConfig { get; set; }

        public ButtonConfig OpenExternalLesson { get; set; }

        private List<StudentLessonEntity> GetExternalLessons(StudentEntity student) {
            var studentGroupsIds = student.Groups.Select(group => group.Id).AsEnumerable();
            return LocalDbContext
                .Instance.StudentLessons
                .Where(studentLesson => studentLesson.Student.Id == student.Id)
                .Where(studentLesson => studentLesson.Lesson._GroupId > 0
                    ? studentGroupsIds.All(studentGroupId => studentGroupId != studentLesson.Lesson.Group.Id)
                    : studentLesson.Lesson.Stream.Groups.All(
                        group => studentGroupsIds.All(studentGroupId => studentGroupId != group.Id)
                    ))
                .ToList();
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public void UpdateLessonMark() {
            var allLessons = new List<StudentLessonEntity>(this.StudentLessons.Select(box => box.StudentLesson));
            allLessons.AddRange(this.ExternalLessons);
            var markStatistics = allLessons.Aggregate
                (
                    new Dictionary<string, int>(),
                    (markStat, model) => {
                        var mark1 = model.Mark;
                        if (string.IsNullOrWhiteSpace(mark1))
                            return markStat;
                        if (markStat.ContainsKey(mark1))
                            markStat[mark1]++;
                        else
                            markStat.Add(mark1, 1);

                        return markStat;
                    }
                )
                .Select(pair => new MarkStatistics(pair.Key, pair.Value))
                .ToArray();
            this.NumberMarkStatistics.Clear();
            this.StringMarkStatistics.Clear();
            this.NumberMarkStatistics.AddRange
            (
                markStatistics
                    .Where(statistics => statistics.MarkAsNumber != -1)
                    .OrderBy(statistics => statistics.MarkAsNumber)
                    .ToList()
            );
            this.StringMarkStatistics.AddRange
            (
                markStatistics
                    .Where(statistics => statistics.MarkAsNumber == -1)
                    .OrderBy(statistics => statistics.Mark)
                    .ToList()
            );
            if (this.NumberMarkStatistics.Count > 0)
                this.AverageMark =
                    this.NumberMarkStatistics.Aggregate
                        (0.0, (i, statistics) => i + statistics.MarkAsNumber)
                    / this.NumberMarkStatistics.Count;
        }

        public List<ButtonConfig> GetControls() {
            var buttonConfigs = new List<ButtonConfig> {
                GetRefreshButtonConfig(),
                new ButtonConfig {
                    Command = new CommandHandler(() => {
                        _host.AddPageAsync(new StudentFormToken("Редактирование " + this.Student.LastName, this.Student));
                    }),
                    Text = "Редактировать"
                }
            };
            return buttonConfigs;
        }

        public void UpdateExamMark() {
            var attestationClearCount = 0;
            var attestationSum = this.StudentAttestations.Aggregate
            (
                0.0,
                (markSum, view) => {
                    if (!int.TryParse(view.Mark, out var i))
                        return markSum;
                    attestationClearCount++;
                    return markSum + i;
                }
            );
            this.ResultAttestationMark = attestationClearCount > 0
                ? (attestationSum / attestationClearCount).ToString
                (
                    CultureInfo.InvariantCulture
                )
                : "";

            var exam = this.StudentExams.FirstOrDefault();
            if (exam == null)
                return;
            if (int.TryParse(exam.Mark, out var mark))
                this.ResultMark =
                    (attestationClearCount > 0
                        ? Math.Round(mark * 0.6 + attestationSum * 0.4 / attestationClearCount)
                        : mark)
                    .ToString(CultureInfo.InvariantCulture);
            else
                this.ResultMark = exam.Mark;
        }

        private async Task AddAttestation() {
            var schedules = await LocalDbContext.Instance.Schedules.ToListAsync();
            var lesson = new LessonEntity();
            var now = DateTime.Now;
            var time = now.TimeOfDay;
            var examSchedule =
                schedules.OrderBy(model => model.Begin)
                    .FirstOrDefault
                    (
                        schedule =>
                            schedule.Begin > time || (schedule.Begin < time && schedule.End > time)
                    );
            lesson.Schedule = examSchedule;
            lesson.Date = now;
            lesson.LessonType = LessonType.Attestation;
            lesson.Stream = this.SelectedStream;
            lesson.Group = this.SelectedGroup;
            lesson._Order = this.StudentExams.Count + 1;
            lesson.CreationDate = now;
            lesson._Checked = 0;
            LocalDbContext.Instance.Lessons.Add(lesson);
            await LocalDbContext.Instance.SaveChangesAsync();
            if (this.SelectedGroup != null) {
                var newStudentLessons = this.SelectedGroup.Students.Select
                    (
                        groupStudent => new StudentLessonEntity {
                            Student = groupStudent,
                            Lesson = lesson
                        }
                    )
                    .ToList();
                LocalDbContext.Instance.StudentLessons.AddRange(newStudentLessons);

                this.StudentAttestations.Add
                (
                    new StudentAttestationExamView
                    (
                        newStudentLessons.FirstOrDefault
                        (
                            studentLesson =>
                                studentLesson.Student == this.Student
                                && studentLesson.Lesson == lesson
                        ),
                        this,
                        this.StudentAttestations.Count + 1
                    )
                );
            }

            await LocalDbContext.Instance.SaveChangesAsync();
            UpdateExamMark();
        }

        private async Task AddExam() {
            var schedules = await LocalDbContext.Instance.Schedules.ToListAsync();
            var lesson = new LessonEntity();
            var now = DateTime.Now;
            var time = now.TimeOfDay;
            var examSchedule =
                schedules.OrderBy(model => model.Begin)
                    .FirstOrDefault
                    (
                        schedule =>
                            schedule.Begin > time || (schedule.Begin < time && schedule.End > time)
                    );
            lesson.Schedule = examSchedule;
            lesson.Date = now;
            lesson.LessonType = LessonType.Exam;
            lesson.Stream = this.SelectedStream;
            lesson.Group = this.SelectedGroup;
            lesson._Order = this.StudentExams.Count + 1;
            lesson.CreationDate = now;
            lesson._Checked = 0;
            LocalDbContext.Instance.Lessons.Add(lesson);
            await LocalDbContext.Instance.SaveChangesAsync();


            if (this.SelectedGroup != null) {
                var newStudentLessons = this.SelectedGroup.Students.Select
                    (
                        groupStudent => new StudentLessonEntity {
                            Student =
                                groupStudent,
                            Lesson = lesson
                        }
                    )
                    .ToArray();
                LocalDbContext.Instance.StudentLessons.AddRange(newStudentLessons);

                this.StudentExams.Add
                (
                    new StudentAttestationExamView
                    (
                        newStudentLessons.FirstOrDefault
                        (
                            studentLesson =>
                                studentLesson.Student == this.Student
                                && studentLesson.Lesson == lesson
                        ),
                        this,
                        0
                    )
                );
            }

            await LocalDbContext.Instance.SaveChangesAsync();
            UpdateExamMark();
        }

        public StudentLessonViewBox ToggleRegistration(StudentLessonViewBox box) {
            box.StudentLesson.IsRegistered = box.StudentLesson.IsLessonMissed;
            var indexOf = this.StudentLessons.IndexOf(box);
            LocalDbContext.Instance.SaveChangesAsync();
            var studentLessonViewBox = new StudentLessonViewBox(box.StudentLesson, this);
            this.StudentLessons.Replace(box, studentLessonViewBox);
            return studentLessonViewBox;
        }
    }
}

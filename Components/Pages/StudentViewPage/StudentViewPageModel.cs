using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Containers;
using Model.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TeacherAssistant.Components;
using TeacherAssistant.ComponentsImpl;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.State;

namespace TeacherAssistant.StudentViewPage {
    public class StudentViewPageModel : AbstractModel {
        private const string LocalizationKey = "page.student.view";
        private readonly IPhotoService _photoService;

        public StudentViewPageModel(string id, IPhotoService photoService) : base(id) {
            _photoService = photoService;

            this.AddAttestationButtonConfig = new ButtonConfig {
                Command = new CommandHandler(AddAttestation),
                Text = "+"
            };
            this.AddExamButtonConfig = new ButtonConfig {
                Command = new CommandHandler(AddExam),
                Text = "+"
            };
            this.WhenAnyValue(model => model.StudentNotes)
                .Subscribe
                (
                    notes => {
                        InterpolateLocalization("page.student.view.student.notes", notes.Count);
                        this.IsStudentNotesEnabled = notes.Count > 0;
                    }
                );
            this.WhenAnyValue(model => model.StudentLessonNotes)
                .Subscribe
                (
                    notes => {
                        InterpolateLocalization("page.student.view.lesson.notes", notes.Count);
                        this.IsStudentLessonNotesEnabled = notes.Count > 0;
                    }
                );
            this.WhenAnyValue(model => model.ExternalLessons)
                .Subscribe
                (
                    lessons => {
                        this.ExternalLessonsVisibility = lessons.Count > 0;
                        InterpolateLocalization("page.student.view.external.lessons", lessons.Count);
                    }
                );
            this.WhenAnyValue(model => model.SelectedStream)
                .Subscribe
                (
                    stream => {
                        if (stream == null) {
                            this.StreamDataVisibility = Visibility.Hidden;
                            return;
                        }

                        this.StreamDataVisibility = Visibility.Visible;
                        InterpolateLocalization
                        (
                            "page.student.view.stream.course",
                            stream.Course.HasValue ? stream.Course.ToString() : ""
                        );
                        InterpolateLocalization
                        (
                            "page.student.view.discipline.lessons",
                            stream.LectureCount,
                            stream.PracticalCount,
                            stream.LabCount
                        );
                        var missedLessons =
                            _db.GetStudentMissedLessons(this.Student, stream, DateTime.Now);
                        var missedLectures = missedLessons.Count
                            (model => model.Lesson.LessonType == LessonType.Lecture);
                        var missedPractices = missedLessons.Count
                            (model => model.Lesson.LessonType == LessonType.Practice);
                        var missedLabs = missedLessons.Count
                            (model => model.Lesson.LessonType == LessonType.Laboratory);
                        var total = missedLectures + missedPractices + missedLabs;
                        this.StudentMissedLessons = total == 0
                            ? Localization["Нет пропущенных занятий"]
                            : Interpolate
                            (
                                "page.student.view.missed.lessons",
                                total,
                                missedLectures,
                                missedPractices,
                                missedLabs
                            );
                    }
                );
            this.WhenAnyValue(model => model.SelectedGroup)
                .Subscribe
                (
                    async group => {
                        if (group == null) {
                            this.StudentGroups.Clear();
                            return;
                        }

                        var groupLessons = await _db.GetGroupLessons(group).ToListAsync();
                        this.SelectedStream = _db.StreamModels.Include(model => model._Discipline)
                            .FirstOrDefault
                            (
                                model => model.Groups.Any
                                (
                                    groupModel =>
                                        groupModel.Id == group.Id
                                )
                            );
                        var lessons = (await _db.StudentLessonModels
                                .Where(lessonModel => this.Student.Id == lessonModel._Student.Id)
                                .ToListAsync())
                            .Where
                            (
                                lessonModel => groupLessons.Any(model => model.Id == lessonModel._Lesson.Id)
                            )
                            .ToArray(); // to init lazy sequence
                        this.StudentLessons.Clear();
                        this.StudentLessons.AddRange
                        (
                            lessons.Where(view => view._Lesson.LessonType < LessonType.Attestation)
                                .Select(model => new StudentLessonViewBox(model, this))
                                .ToList()
                        );
                        var attestations = 0;
                        var exams = 0;
                        this.StudentAttestations.Clear();
                        this.StudentAttestations.AddRange
                        (
                            lessons.Where(view => view.Lesson.LessonType == LessonType.Attestation)
                                .Select(model => new StudentAttestationExamView(model, this, ++attestations))
                                .OrderBy(view => view.StudentLesson.Lesson._Order)
                                .ToList()
                        );
                        this.StudentExams.Clear();
                        this.StudentExams.AddRange
                        (
                            lessons.Where(view => view.Lesson.LessonType == LessonType.Exam)
                                .Select
                                    (model => new StudentAttestationExamView(model, this, ++exams))
                                .OrderBy(view => view.StudentLesson.Lesson._Order)
                                .ToList()
                        );
                        this.AddExamButtonConfig.IsEnabled = this.StudentExams.Count < 1;
                        UpdateLessonMark();
                        UpdateExamMark();
                    }
                );
        }

        [Reactive] public BitmapImage StudentPhoto { get; set; }
        [Reactive] public string Groups { get; set; }
        [Reactive] public StudentModel Student { get; set; }

        public ObservableRangeCollection<StudentLessonModel> ExternalLessons { get; set; } =
            new WpfObservableRangeCollection<StudentLessonModel>();

        [Reactive] public bool ExternalLessonsVisibility { get; set; }

        public ObservableRangeCollection<StudentNote> StudentNotes { get; set; } =
            new WpfObservableRangeCollection<StudentNote>();

        public ObservableRangeCollection<StudentLessonNote> StudentLessonNotes { get; set; } =
            new WpfObservableRangeCollection<StudentLessonNote>();

        public ObservableRangeCollection<StudentLessonViewBox> StudentLessons { get; set; } =
            new WpfObservableRangeCollection<StudentLessonViewBox>();

        public ObservableRangeCollection<StudentAttestationExamView> StudentAttestations { get; set; } =
            new WpfObservableRangeCollection<StudentAttestationExamView>();

        public ObservableRangeCollection<StudentAttestationExamView> StudentExams { get; set; } =
            new WpfObservableRangeCollection<StudentAttestationExamView>();

        [Reactive] public bool IsStudentNotesEnabled { get; set; }

        [Reactive] public bool IsStudentLessonNotesEnabled { get; set; }

        [Reactive] public bool IsStudentGroupsSelectorEnabled { get; set; }
        [Reactive] public Visibility StreamDataVisibility { get; set; }

        [Reactive] public GroupModel SelectedGroup { get; set; }

        [Reactive] public StreamModel SelectedStream { get; set; }

        public ObservableRangeCollection<GroupModel> StudentGroups { get; set; } =
            new WpfObservableRangeCollection<GroupModel>();

        [Reactive] public string StudentMissedLessons { get; set; } = "";

        [Reactive] public double AverageMark { get; set; }

        public ObservableRangeCollection<MarkStatistics> NumberMarkStatistics { get; set; } =
            new WpfObservableRangeCollection<MarkStatistics>();

        public ObservableRangeCollection<MarkStatistics> StringMarkStatistics { get; set; } =
            new WpfObservableRangeCollection<MarkStatistics>();

        [Reactive] public string ResultAttestationMark { get; set; }
        [Reactive] public string ResultMark { get; set; }
        public ButtonConfig AddAttestationButtonConfig { get; set; }
        public ButtonConfig AddExamButtonConfig { get; set; }

        private List<StudentLessonModel> GetExternalLessons(StudentModel student) {
            var studentGroups = student.Groups.AsEnumerable();
            return _db.StudentLessonModels
                .Where(model => model._Student.Id == student.Id)
                .Include(model => model._Lesson._Group)
                .AsEnumerable()
                .Where
                (
                    model => model._Lesson != null
                             && (model._Lesson._Group != null
                                 ? studentGroups.All
                                 (
                                     groupModel => groupModel.Id != model._Lesson._Group.Id
                                 )
                                 : !_db.StreamModels
                                       .FirstOrDefault
                                       (
                                           stream => stream.Id == model._Lesson._Stream.Id
                                       )
                                       ?.Groups
                                       //   .AsEnumerable() // EF cannot manipulate collections properly
                                       .Any
                                       (
                                           groupModel =>
                                               studentGroups.Any
                                               (
                                                   model1 => model1.Id == groupModel.Id
                                               )
                                       )
                                   ?? false)
                )
                .ToList();
        }

        protected override string GetLocalizationKey() {
            return LocalizationKey;
        }

        public void UpdateLessonMark() {
            var allLessons = new List<StudentLessonModel>(this.StudentLessons.Select(box => box.StudentLesson));
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

        public override List<ButtonConfig> GetControls() {
            var buttonConfigs = base.GetControls();
            buttonConfigs.Add(new ButtonConfig {
                Command = new CommandHandler
                (
                    () => {
                        var id = this.PageService.OpenPage
                        (
                            new PageProperties {
                                MinHeight = 600,
                                Header = "Редактирование " + this.Student.LastName,
                                PageType = typeof(StudentForm.StudentForm)
                            },
                            this.Id
                        );
                        StoreManager.Publish(this.Student, id, "Student");
                    }
                ),
                Text = "Редактировать"
            });
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
            var schedules = await _db.ScheduleModels.ToListAsync();
            var lesson = new LessonModel();
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
            lesson._Stream = this.SelectedStream;
            lesson._Group = this.SelectedGroup;
            lesson._Order = this.StudentExams.Count + 1;
            lesson.CreationDate = now;
            lesson._Checked = 0;
            _db.LessonModels.Add(lesson);
            await _db.SaveChangesAsync();
            if (this.SelectedGroup != null) {
                var newStudentLessons = this.SelectedGroup.Students.Select
                    (
                        groupStudent => new StudentLessonModel {
                            Student = groupStudent,
                            Lesson = lesson
                        }
                    )
                    .ToList();
                _db.StudentLessonModels.AddRange(newStudentLessons);

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

            await _db.SaveChangesAsync();
            UpdateExamMark();
        }

        private async Task AddExam() {
            var schedules = await _db.ScheduleModels.ToListAsync();
            var lesson = new LessonModel();
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
            lesson._Stream = this.SelectedStream;
            lesson._Group = this.SelectedGroup;
            lesson._Order = this.StudentExams.Count + 1;
            lesson.CreationDate = now;
            lesson._Checked = 0;
            _db.LessonModels.Add(lesson);
            await _db.SaveChangesAsync();


            if (this.SelectedGroup != null) {
                var newStudentLessons = this.SelectedGroup.Students.Select
                    (
                        groupStudent => new StudentLessonModel {
                            Student =
                                groupStudent,
                            Lesson = lesson
                        }
                    )
                    .ToArray();
                _db.StudentLessonModels.AddRange(newStudentLessons);

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

            await _db.SaveChangesAsync();
            UpdateExamMark();
        }

        public StudentLessonViewBox ToggleRegistration(StudentLessonViewBox box) {
            box.StudentLesson.IsRegistered = box.StudentLesson.IsLessonMissed;
            var indexOf = this.StudentLessons.IndexOf(box);
            _db.SaveChangesAsync();
            var studentLessonViewBox = new StudentLessonViewBox(box.StudentLesson, this);
            this.StudentLessons.ReplaceRange(indexOf, 1, new[] {studentLessonViewBox});
            return studentLessonViewBox;
        }

        public override Task Init() {
            Select<StudentModel>(this.Id, "Student")
                .Subscribe
                (
                    async model => {
                        if (model == null)
                            return;

                        this.Student = model;
                        this.Groups = string.Join(", ", model.Groups.Select(group => group.Name));
                        this.ExternalLessons.Clear();
                        this.ExternalLessons.AddRange(GetExternalLessons(model));
                        this.StudentGroups.Clear();
                        this.StudentGroups.AddRange(model.Groups);
                        this.IsStudentGroupsSelectorEnabled = this.StudentGroups.Count > 1;
                        this.SelectedGroup = this.StudentGroups.Count > 0
                            ? (this.StudentGroups.FirstOrDefault(groupModel => groupModel.IsActive)
                               ?? this.StudentGroups[0])
                            : null;
                        this.StudentNotes.Clear();
                        //this.StudentNotes.AddRange(model.Notes.ToList());
                        this.StudentLessonNotes.Clear();
                        this.StudentLessonNotes.AddRange
                        (
                            await _db.StudentLessonNotes.Include
                                    (note => note.StudentLesson)
                                .Where(note => note.StudentLesson._StudentId == model.Id)
                                .ToListAsync()
                        );
                        var path = await _photoService.DownloadPhoto
                                (StudentModel.CardUidToId(model.CardUid))
                            .ConfigureAwait(false);
                        var image = _photoService.GetImage(path);
                        UpdateFromAsync(() => { this.StudentPhoto = image; });
                    }
                );
            return Task.CompletedTask;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EntityFramework.Triggers;
using Model;
using Model.Models;
using SQLite.CodeFirst;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Models;

namespace TeacherAssistant.Database {
    public class LocalDbContext : DbContextWithTriggers {
        public const string DatabaseExtension = ".s3db";
        private readonly Subject<object> _delayedUpdateStart;

        public LocalDbContext(DbConnection connection) : base(connection,true) {
            this._delayedUpdateStart = new Subject<object>();
            this._delayedUpdateStart
                .Throttle(TimeSpan.FromMilliseconds(5000))
                .Subscribe(_ => { SaveChanges(); });
        }

        public DbSet<DepartmentEntity> Departments { get; set; }
        public DbSet<AlarmEntity> Alarms { get; set; }

        public DbSet<StudentEntity> Students { get; set; }

        //  public DbSet<StreamGroupModel> StreamGroupModels { get; set; }
        public DbSet<StudentLessonEntity> StudentLessons { get; set; }
        public DbSet<StreamEntity> Streams { get; set; }
        public DbSet<LessonEntity> Lessons { get; set; }
        public DbSet<LessonTypeEntity> LessonTypes { get; set; }
        public DbSet<GroupEntity> Groups { get; set; }
        public DbSet<DisciplineEntity> Disciplines { get; set; }

        public DbSet<LessonNote> LessonNotes { get; set; }
        public DbSet<StudentNote> StudentNotes { get; set; }
        public DbSet<StudentLessonNote> StudentLessonNotes { get; set; }

        //      public DbSet<StudentGroupModel> StudentGroupModels { get; set; }
        public DbSet<ScheduleEntity> Schedules { get; set; }

        public void ThrottleSave() {
            this._delayedUpdateStart.OnNext(0);
        }

        public long GetDatabaseVersion() {
            return this.Database.SqlQuery<long>("PRAGMA user_version;").First();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<LocalDbContext>(modelBuilder);
            System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
            ModelConfiguration.ModelConfiguration.Configure(modelBuilder);
        }

        public IQueryable<LessonEntity> GetGroupLessons(GroupEntity group) {
            return this.Lessons
                .Where
                (
                    model => model.Group != null && model.Group.Id == group.Id
                             || model.Stream.Groups.Any(streamGroup => streamGroup.Id == group.Id)
                );
        }

        public List<StudentLessonEntity> GetStudentMissedLessons(
            StudentEntity student,
            StreamEntity stream,
            DateTime until
        ) {
            var lessonModels = stream.StreamLessons?.Where
            (
                lesson =>
                    (lesson.StudentLessons?.Any
                    (
                        studentLesson =>
                            studentLesson.Student?.Id == student.Id
                    ) ?? false)
                    && lesson.LessonType < LessonType.Attestation
                    && lesson.Date < until
            ) ??  new List<LessonEntity>();
            return lessonModels
                .Select
                (
                    lesson => lesson.StudentLessons?.First
                    (
                        studentLesson => studentLesson.Student?.Id == student.Id
                    )
                )
                .Where(studentLesson => studentLesson?.IsLessonMissed ?? false)
                .ToList();
        }

        public void SetLessonOrder(LessonEntity entity) {
            var lessons = this.Lessons.Where(lessonEntity =>
                    lessonEntity._StreamId == entity._StreamId
                    && lessonEntity._GroupId == entity._GroupId
                    && lessonEntity._Date != null
                    && lessonEntity._ScheduleId.HasValue)
                .OrderBy(lessonEntity => lessonEntity._Order)
                .ToList();
            LessonEntity previous = null;
            bool isOrderSet = false;
            foreach (var lessonEntity in lessons) {
                if (isOrderSet) {
                    lessonEntity.Order++;
                    continue;
                }

                var dateCompare = lessonEntity.Date.Value.CompareTo(entity.Date.Value);
                if (dateCompare < 0) {
                    previous = lessonEntity;
                    continue;
                }

                if (dateCompare > 0) {
                    entity.Order = (previous?.Order ?? 0) + 1;
                    isOrderSet = true;
                    continue;
                }

                var compareTo = lessonEntity.Schedule.Begin.Value.CompareTo(entity.Schedule.Begin.Value);
                if (compareTo > 0) {
                    entity.Order = (previous?.Order ?? 0) + 1;
                    isOrderSet = true;
                }

                previous = lessonEntity;
            }

            if (!isOrderSet) {
                entity.Order = 1;
            }
        }
    }
}
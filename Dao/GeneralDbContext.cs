using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EntityFramework.Rx;
using Model;
using Model.Models;
using SQLite.CodeFirst;

namespace TeacherAssistant.Dao
{
    public enum ChangeReason
    {
        Update,
        Insert,
        Delete
    }

    public class DbChange<T>
    {
        public DbChange(T entity, ChangeReason changeReason)
        {
            this.Entity = entity;
            this.ChangeReason = changeReason;
        }

        public T Entity { get; }
        public ChangeReason ChangeReason { get; }
    }

    public class GeneralDbContext : DbContext
    {
        private static GeneralDbContext _instance;

        private GeneralDbContext(string dataSource) : base
        (
            new SQLiteConnection
            {
                ConnectionString = new SQLiteConnectionStringBuilder
                                   {
                                       DataSource = dataSource,
                                       ForeignKeys = true
                                   }
                   .ConnectionString
            },
            true
        )
        {
            Path = dataSource;
            this._delayedUpdateStart = new Subject<object>();
            this._delayedUpdateStart.AsObservable()
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Subscribe(o => { SaveChangesAsync(); });
        }

        public static GeneralDbContext Instance => _instance ?? (_instance = new GeneralDbContext("./db.s3db"));
        public static string Path { get; private set; }
        private Subject<object> _delayedUpdateStart { get; }

        public DbSet<DepartmentModel> DepartmentModels { get; set; }
        public DbSet<AlarmModel> AlarmModels { get; set; }

        public DbSet<StudentModel> StudentModels { get; set; }

        //  public DbSet<StreamGroupModel> StreamGroupModels { get; set; }
        public DbSet<StudentLessonModel> StudentLessonModels { get; set; }
        public DbSet<StreamModel> StreamModels { get; set; }
        public DbSet<LessonModel> LessonModels { get; set; }
        public DbSet<LessonTypeModel> LessonTypeModels { get; set; }
        public DbSet<GroupModel> GroupModels { get; set; }
        public DbSet<DisciplineModel> DisciplineModels { get; set; }

        public DbSet<NoteModel> NoteModels { get; set; }

        //      public DbSet<StudentGroupModel> StudentGroupModels { get; set; }
        public DbSet<ScheduleModel> ScheduleModels { get; set; }
        public static event EventHandler<string> DatabaseChanged;


        public IObservable<IEnumerable<DbChange<T>>> ChangeListener<T>(int delayMs = 1000) where T : class
        {
            var changeSource = Observable.Merge
            (
                DbObservable<GeneralDbContext>
                   .FromInserted<T>()
                   .Select(entry => new DbChange<T>(entry.Entity, ChangeReason.Insert)),
                DbObservable<GeneralDbContext>
                   .FromDeleted<T>()
                   .Select(entry => new DbChange<T>(entry.Entity, ChangeReason.Delete)),
                DbObservable<GeneralDbContext>
                   .FromUpdated<T>()
                   .Select(entry => new DbChange<T>(entry.Entity, ChangeReason.Update))
            );
            var throttle = changeSource.Throttle(TimeSpan.FromMilliseconds(delayMs));
            return changeSource.Buffer(throttle);
        }

        public void ThrottleSave(object o)
        {
            this._delayedUpdateStart.OnNext(o);
        }

        public static void Reconnect(string dataSource)
        {
            _instance?._delayedUpdateStart.Dispose();
            _instance?.Dispose();
            _instance = new GeneralDbContext(dataSource);
            Init();
            DatabaseChanged?.Invoke(_instance, dataSource);
        }

        private static void Init()
        {
            var count = Instance.LessonTypeModels.Count();
            var isDbVersionNotLast = count < 5 && count >= 3;
            if (isDbVersionNotLast)
            {
                Instance.LessonTypeModels.Add(new LessonTypeModel {name = "Аттестация"});
                Instance.LessonTypeModels.Add(new LessonTypeModel {name = "Экзамен"});
                Instance.SaveChanges();
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GeneralDbContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<StudentModel>()
                        .HasMany(model => model.Groups)
                        .WithMany(group => group.Students)
                        .Map
                         (
                             configuration =>
                             {
                                 configuration
                                    .MapLeftKey("student_id")
                                    .MapRightKey("group_id")
                                    .ToTable("STUDENT_GROUP");
                             }
                         );
            modelBuilder.Entity<StreamModel>()
                        .HasMany(model => model.Groups)
                        .WithMany(group => group.Streams)
                        .Map
                         (
                             configuration =>
                             {
                                 configuration
                                    .MapLeftKey("stream_id")
                                    .MapRightKey("group_id")
                                    .ToTable("STREAM_GROUP");
                             }
                         );
            modelBuilder.Entity<StudentModel>()
                        .HasMany(model => model.StudentLessons)
                        .WithRequired(studentLesson => studentLesson.Student)
                        .Map
                         (
                             configuration =>
                             {
                                 configuration
                                    .MapKey("id")
                                    .ToTable("STUDENT_LESSON");
                             }
                         );
            modelBuilder.Entity<LessonModel>()
                        .HasMany(model => model.StudentLessons)
                        .WithRequired(studentLesson => studentLesson.Lesson)
                        .Map
                         (
                             configuration =>
                             {
                                 configuration
                                    .MapKey("id")
                                    .ToTable("STUDENT_LESSON");
                             }
                         );
            modelBuilder.Entity<StreamModel>()
                        .HasMany(model => model.StreamLessons)
                        .WithRequired(lesson => lesson.Stream)
                        .Map
                         (
                             configuration =>
                             {
                                 configuration
                                    .MapKey("id")
                                    .ToTable("LESSON");
                             }
                         );
        }

        public IQueryable<NoteModel> GetStudentNotes(StudentModel student)
        {
            return this.NoteModels
                       .Where
                        (
                            model => model.type.Equals
                                         (NoteType.STUDENT.ToString())
                                     && model.entity_id.HasValue
                                     && model.entity_id == student.id
                        );
        }

        public IQueryable<NoteModel> GetStudentLessonNotes(StudentLessonModel studentLesson)
        {
            return this.NoteModels
                       .Where
                        (
                            model => model.type.Equals
                                         (NoteType.STUDENT_LESSON.ToString())
                                     && model.entity_id.HasValue
                                     && model.entity_id == studentLesson.id
                        );
        }

        public IQueryable<NoteModel> GetStudentLessonNotes(StudentModel student)
        {
            return this.NoteModels
                       .Where
                        (
                            model => model.type.Equals(NoteType.STUDENT_LESSON.ToString())
                                     && model.entity_id.HasValue
                                     && this.StudentLessonModels.Any
                                     (
                                         lessonModel =>
                                             lessonModel
                                                .student_id
                                             == model.id
                                             && lessonModel.id == model.entity_id
                                     )
                        );
        }

        public IQueryable<NoteModel> GetLessonNotes(LessonModel lesson)
        {
            return this.NoteModels
                       .Where
                        (
                            model => model.type.Equals
                                         (NoteType.LESSON.ToString())
                                     && model.entity_id.HasValue
                                     && model.entity_id == lesson.id
                        );
        }


        public IQueryable<LessonModel> GetGroupLessons(GroupModel group)
        {
            return this.LessonModels
                       .Where
                        (
                            model => model.group_id.HasValue && model.group_id == group.id
                                     || model.Stream.Groups.Any(streamGroup => streamGroup.id == group.id)
                        );
        }

        public List<StudentLessonModel> GetStudentMissedLessons
        (
            StudentModel student,
            StreamModel stream,
            DateTime until
        )
        {
            var lessonModels = stream.StreamLessons.Where
            (
                lesson =>
                    lesson.StudentLessons.Any
                    (
                        studentLesson =>
                            studentLesson.student_id == student.id
                    )
                    && lesson.LessonType < LessonType.Attestation
                    && lesson.Date < until
            );
            return lessonModels
                  .Select
                   (
                       lesson => lesson.StudentLessons.First
                       (
                           studentLesson => studentLesson.student_id == student.id
                       )
                   )
                  .Where
                   (
                       studentLesson =>
                           studentLesson.IsLessonMissed
                   )
                  .ToList();
        }
    }
}
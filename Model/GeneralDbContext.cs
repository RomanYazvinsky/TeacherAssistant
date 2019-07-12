using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using EntityFramework.Rx;
using EntityFramework.Triggers;
using Model;
using Model.Models;
using SQLite.CodeFirst;
using TeacherAssistant.Dao.Notes;

namespace TeacherAssistant.Dao {
    public enum ChangeReason {
        Update,
        Insert,
        Delete
    }

    public class DbChange<T> {
        public DbChange(T entity, ChangeReason changeReason) {
            this.Entity = entity;
            this.ChangeReason = changeReason;
        }

        public T Entity { get; }
        public ChangeReason ChangeReason { get; }
    }

    public class GeneralDbContext : DbContextWithTriggers {
        private static GeneralDbContext _instance;

        public static readonly string FixDbScript = @"
        delete from STUDENT_LESSON where not exists (select id from STUDENT where id == STUDENT_LESSON.student_id);
        delete from STUDENT_LESSON where not exists (select id from LESSON where id == STUDENT_LESSON.lesson_id);";

        private GeneralDbContext(string dataSource, DbConnection connection) : base
        (
            connection,
            true
        ) {
            Path = dataSource;
            this._delayedUpdateStart = new Subject<object>();
            this._delayedUpdateStart.AsObservable()
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Subscribe(o => {
                    SaveChanges();
                });
        }

        public static GeneralDbContext Instance {
            get {
                if (_instance != null) {
                    return _instance;
                }

                var dbConnection = SQLiteFactory.Instance.CreateConnection();
                dbConnection.ConnectionString = new SQLiteConnectionStringBuilder {
                        DataSource = "./db.s3db",
                        ForeignKeys = true
                    }
                    .ConnectionString;
                return _instance = new GeneralDbContext("./db.s3db", dbConnection);
            }
        }

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

        public DbSet<LessonNote> LessonNotes { get; set; }
        public DbSet<StudentNote> StudentNotes { get; set; }
        public DbSet<StudentLessonNote> StudentLessonNotes { get; set; }

        //      public DbSet<StudentGroupModel> StudentGroupModels { get; set; }
        public DbSet<ScheduleModel> ScheduleModels { get; set; }
        public static event EventHandler<string> DatabaseChanged;


        public IObservable<IEnumerable<DbChange<T>>> ChangeListener<T>(int delayMs = 1000) where T : class {
            var changeSource = Observable.Merge
            (
                DbObservable<GeneralDbContext>
                    .FromInserting<T>()
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

        public void ThrottleSave() {
            this._delayedUpdateStart.OnNext(0);
        }

        public static void Reconnect(string dataSource) {
            _instance?._delayedUpdateStart.Dispose();
            _instance?.Database.Connection.Close();
            _instance?.Dispose();
            var dbConnection = SQLiteFactory.Instance.CreateConnection();
            dbConnection.ConnectionString = new SQLiteConnectionStringBuilder {
                    DataSource = dataSource,
                    ForeignKeys = true
                }
                .ConnectionString;
            _instance = new GeneralDbContext(dataSource, dbConnection);
            Init();
            DatabaseChanged?.Invoke(_instance, dataSource);
        }

        private static void Init() {
//            Task.Run(() => {
                var count = Instance.LessonTypeModels.Count();
                var isDbVersionNotLast = count < 5 && count >= 3;
                if (isDbVersionNotLast) {
                    Instance.LessonTypeModels.Add(new LessonTypeModel {Name = "Аттестация"});
                    Instance.LessonTypeModels.Add(new LessonTypeModel {Name = "Экзамен"});
                    Instance.SaveChanges();
                }

                // TODO optional
                Instance.Database.ExecuteSqlCommand(FixDbScript);
                Instance.Database.ExecuteSqlCommand("PRAGMA foreign_keys=off;");
                Instance.Database.ExecuteSqlCommand
                (
                    @"
                CREATE TABLE GROUP_TYPE2 (
                     id INTEGER PRIMARY KEY AUTOINCREMENT,
                     name TEXT
                );
                INSERT INTO GROUP_TYPE2 (id, name)
                    SELECT id, name FROM GROUP_TYPE;
                DROP TABLE GROUP_TYPE;
                ALTER TABLE GROUP_TYPE2 RENAME TO GROUP_TYPE;
                PRAGMA foreign_keys=off;
                CREATE TABLE STUDENT_LESSON2 (
	                id	INTEGER PRIMARY KEY AUTOINCREMENT,
	                student_id	INTEGER,
	                lesson_id	INTEGER,
	                registered	INTEGER DEFAULT 0,
	                registration_time	TEXT,
	                registration_type	TEXT,
	                mark	TEXT,
	                mark_time	TEXT,
	                FOREIGN KEY(lesson_id) REFERENCES LESSON(id) ON DELETE CASCADE,
	                FOREIGN KEY(student_id) REFERENCES STUDENT(id) ON DELETE CASCADE
                );
                INSERT INTO STUDENT_LESSON2
                    SELECT * FROM STUDENT_LESSON;
                DROP TABLE STUDENT_LESSON;
                ALTER TABLE STUDENT_LESSON2 RENAME TO STUDENT_LESSON;
"
                );

                Instance.Database.ExecuteSqlCommand("PRAGMA foreign_keys=on;");
//            });
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GeneralDbContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Entity<StudentModel>()
                .HasMany(model => model.Groups)
                .WithMany(group => group.Students)
                .Map
                (
                    configuration => {
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
                    configuration => {
                        configuration
                            .MapLeftKey("stream_id")
                            .MapRightKey("group_id")
                            .ToTable("STREAM_GROUP");
                    }
                );
            modelBuilder.Entity<StudentLessonModel>()
                .HasRequired(model => model._Student)
                .WithMany(model => model.StudentLessons)
                .Map
                (
                    configuration => {
                        configuration
                            .MapKey("student_id")
                            .ToTable("STUDENT_LESSON");
                    }
                )
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<LessonModel>()
                .HasMany(model => model.StudentLessons)
                .WithRequired(model => model._Lesson)
                .Map
                (
                    configuration => {
                        configuration
                            .MapKey("lesson_id")
                            .ToTable("STUDENT_LESSON");
                    }
                )
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<StreamModel>()
                .HasMany(model => model.StreamLessons)
                .WithRequired(lesson => lesson._Stream)
                .Map
                (
                    configuration => {
                        configuration
                            .MapKey("id")
                            .ToTable("LESSON");
                    }
                )
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<NoteModel>()
                .Map<LessonNote>(model => model.Requires("type").HasValue(NoteType.LESSON.ToString()));
            modelBuilder.Entity<NoteModel>()
                .Map<StudentNote>(model => model.Requires("type").HasValue(NoteType.STUDENT.ToString()));
            modelBuilder.Entity<NoteModel>()
                .Map<StudentLessonNote>
                    (model => model.Requires("type").HasValue(NoteType.STUDENT_LESSON.ToString()));

            modelBuilder.Entity<StudentModel>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.Student)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("STUDENT"); })
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<LessonModel>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.Lesson)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("LESSON"); })
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<StudentLessonModel>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.StudentLesson)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("STUDENT_LESSON"); })
                .WillCascadeOnDelete(true);
        }

        public IQueryable<LessonModel> GetGroupLessons(GroupModel group) {
            return this.LessonModels
                .Where
                (
                    model => model._Group != null && model._Group.Id == group.Id
                             || model._Stream.Groups.Any(streamGroup => streamGroup.Id == group.Id)
                );
        }

        public List<StudentLessonModel> GetStudentMissedLessons(
            StudentModel student,
            StreamModel stream,
            DateTime until
        ) {
            var lessonModels = stream.StreamLessons.Where
            (
                lesson =>
                    lesson.StudentLessons.Any
                    (
                        studentLesson =>
                            studentLesson.Student?.Id == student.Id
                    )
                    && lesson.LessonType < LessonType.Attestation
                    && lesson.Date < until
            );
            return lessonModels
                .Select
                (
                    lesson => lesson.StudentLessons.First
                    (
                        studentLesson => studentLesson.Student?.Id == student.Id
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
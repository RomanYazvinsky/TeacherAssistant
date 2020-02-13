using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using EntityFramework.Rx;
using EntityFramework.Triggers;
using Model;
using Model.Models;
using SQLite.CodeFirst;
using TeacherAssistant.Dao.Notes;
using TeacherAssistant.Models;

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

    public class LocalDbContext : DbContextWithTriggers {
        public const string MpegStartMetadata = "ID3"; // ID3v2 start metadata
        public const string WavStartMetadata = "RIFF"; // wav start metadata

        private static LocalDbContext _instance;

        public static readonly string FixDbScript = @"
        delete from STUDENT_LESSON where not exists (select id from STUDENT where id == STUDENT_LESSON.student_id);
        delete from STUDENT_LESSON where not exists (select id from LESSON where id == STUDENT_LESSON.lesson_id);
        ";

        private LocalDbContext(string dataSource, DbConnection connection) : base
        (
            connection,
            true
        ) {
            Path = dataSource;
            this._delayedUpdateStart = new Subject<object>();
            this._delayedUpdateStart.AsObservable()
                .Throttle(TimeSpan.FromMilliseconds(1000))
                .Subscribe(o => { SaveChanges(); });
        }

        public static LocalDbContext Instance {
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
                return _instance = new LocalDbContext("./db.s3db", dbConnection);
            }
        }

        public static string Path { get; private set; }
        private Subject<object> _delayedUpdateStart { get; }

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
        public static event EventHandler<string> DatabaseChanged;


        public IObservable<IEnumerable<DbChange<T>>> ChangeListener<T>(int delayMs = 1000) where T : class {
            var changeSource = Observable.Merge
            (
                DbObservable<LocalDbContext>
                    .FromInserting<T>()
                    .Select(entry => new DbChange<T>(entry.Entity, ChangeReason.Insert)),
                DbObservable<LocalDbContext>
                    .FromDeleted<T>()
                    .Select(entry => new DbChange<T>(entry.Entity, ChangeReason.Delete)),
                DbObservable<LocalDbContext>
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
            try
            {
                _instance = new LocalDbContext(dataSource, dbConnection);
                _instance.Database.Exists();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            // TODO remove event from singleton
            DatabaseChanged?.Invoke(_instance, dataSource);
        }

        // TODO optional
        private static void Init() {
            var count = Instance.LessonTypes.Count();
            var isDbVersionNotLast = count < 5 && count >= 3;
            if (isDbVersionNotLast) {
                Instance.LessonTypes.Add(new LessonTypeEntity {Name = "Аттестация"});
                Instance.LessonTypes.Add(new LessonTypeEntity {Name = "Экзамен"});
                Instance.SaveChanges();
            }

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

            ");

            Instance.Database.ExecuteSqlCommand(@"
                CREATE TABLE `ALARM2` (
	                `id`	INTEGER PRIMARY KEY AUTOINCREMENT,
	                `active`	INTEGER DEFAULT 0,
	                `time`	INTEGER,
	                `volume`	DECIMAL ( 1 , 1 ),
	                `sound`	BLOB,
	                `discriminator` TEXT,
	                `resource_name` TEXT
                );
                
                INSERT INTO ALARM2 (id, active, time, volume, sound)
                    SELECT id, active, time, volume, sound FROM ALARM;
                
                DROP TABLE ALARM;
                ALTER TABLE ALARM2 RENAME TO ALARM;
            ");

            Instance.Database.ExecuteSqlCommand("PRAGMA foreign_keys=on;");
        }

        private void TrySetAudioDiscriminator() {
            var alarms = this.Alarms.ToList();
            foreach (var alarmEntity in alarms) {
                var sound = alarmEntity.Sound;
                if (sound == null || sound.Length == 0 || !string.IsNullOrWhiteSpace(alarmEntity.Discriminator)) {
                    continue;
                }

                var metadataStart = new byte[20];
                Array.Copy(sound, metadataStart, 20);
                string metadataAsString = Encoding.ASCII.GetString(metadataStart);
                if (metadataAsString.StartsWith(MpegStartMetadata)) {
                    alarmEntity.Discriminator = ".mp3";
                }

                if (metadataAsString.StartsWith(WavStartMetadata)) {
                    alarmEntity.Discriminator = ".wav";
                }
            }

            SaveChangesAsync();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<LocalDbContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Entity<StudentEntity>()
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
            modelBuilder.Entity<StreamEntity>()
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
            modelBuilder.Entity<StudentLessonEntity>()
                .HasRequired(model => model.Student)
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
            modelBuilder.Entity<LessonEntity>()
                .HasMany(model => model.StudentLessons)
                .WithRequired(model => model.Lesson)
                .Map
                (
                    configuration => {
                        configuration
                            .MapKey("lesson_id")
                            .ToTable("STUDENT_LESSON");
                    }
                )
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<StreamEntity>()
                .HasMany(model => model.StreamLessons)
                .WithRequired(lesson => lesson.Stream)
                .Map
                (
                    configuration => {
                        configuration
                            .MapKey("id")
                            .ToTable("LESSON");
                    }
                )
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<NoteEntity>()
                .Map<LessonNote>(model => model.Requires("type").HasValue(NoteType.LESSON.ToString()));
            modelBuilder.Entity<NoteEntity>()
                .Map<StudentNote>(model => model.Requires("type").HasValue(NoteType.STUDENT.ToString()));
            modelBuilder.Entity<NoteEntity>()
                .Map<StudentLessonNote>
                    (model => model.Requires("type").HasValue(NoteType.STUDENT_LESSON.ToString()));

            modelBuilder.Entity<StudentEntity>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.Student)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("STUDENT"); })
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<LessonEntity>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.Lesson)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("LESSON"); })
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<StudentLessonEntity>()
                .HasMany(model => model.Notes)
                .WithRequired(note => note.StudentLesson)
                .Map(
                    configuration => { configuration.MapKey("EntityId").ToTable("STUDENT_LESSON"); })
                .WillCascadeOnDelete(true);
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
                .Where(studentLesson => studentLesson.IsLessonMissed)
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

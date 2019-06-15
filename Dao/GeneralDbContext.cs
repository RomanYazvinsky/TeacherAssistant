using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using Model;
using Model.Models;
using SQLite.CodeFirst;

namespace Dao
{
    public class GeneralDbContext : DbContext
    {
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
  //      public DbSet<StudentGroupModel> StudentGroupModels { get; set; }
        public DbSet<ScheduleModel> ScheduleModels { get; set; }
        private static GeneralDbContext _instance;
        public static event EventHandler<string> DatabaseChanged;

        public static GeneralDbContext Instance => _instance ?? (_instance = new GeneralDbContext("./db.s3db"));

        private GeneralDbContext(string dataSource) : base(
            new SQLiteConnection
            {
                ConnectionString = new SQLiteConnectionStringBuilder
                    {
                        DataSource = dataSource,
                        ForeignKeys = true
                    }
                    .ConnectionString
            }, true)
        {
        }

        public static void Reconnect(string dataSource)
        {
            _instance?.Dispose();
            _instance = new GeneralDbContext(dataSource);
            DatabaseChanged?.Invoke(_instance, dataSource);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GeneralDbContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<StudentModel>().HasMany(model => model.Groups)
                .WithMany(model => model.Students).Map(configuration =>
                {
                    configuration.MapLeftKey("student_id").MapRightKey("group_id").ToTable("STUDENT_GROUP");
                });
            modelBuilder.Entity<StreamModel>().HasMany(model => model.Groups)
                .WithMany(model => model.Streams).Map(configuration =>
                {
                    configuration.MapLeftKey("stream_id").MapRightKey("group_id").ToTable("STREAM_GROUP");
                });
        }
    }
}
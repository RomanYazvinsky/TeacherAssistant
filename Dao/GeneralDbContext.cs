using System.Data.Entity;
using System.Data.SQLite;
using Model;
using Model.Models;
using SQLite.CodeFirst;

namespace Dao
{
    public class GeneralDbContext : DbContext
    {
        public DbSet<DepartmentModel> DepartmentModels { get; set; }
        public DbSet<StudentModel> StudentModels { get; set; }
        public DbSet<StreamGroupModel> StreamGroupModels { get; set; }
        public DbSet<StudentLessonModel> StudentLessonModels { get; set; }
        public DbSet<StreamModel> StreamModels { get; set; }
        public DbSet<LessonModel> LessonModels { get; set; }
        public DbSet<GroupModel> GroupModels { get; set; }
        public DbSet<StudentGroupModel> StudentGroupModels { get; set; }
        private static GeneralDbContext _instance;


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

        public static GeneralDbContext GetInstance(string dataSource = "./db.s3db")
        {
            if (dataSource != "./db.s3db")
            {
                _instance?.Dispose();
                return _instance = new GeneralDbContext(dataSource);
            }
            return _instance ?? (_instance = new GeneralDbContext(dataSource));
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GeneralDbContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }

    }
}
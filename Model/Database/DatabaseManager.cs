using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NLog;
using TeacherAssistant.Helpers.Exceptions;
using TeacherAssistant.Migrations;
using TeacherAssistant.Properties;

namespace TeacherAssistant.Database {
    public class DatabaseManager : IDisposable {
        private readonly IEnumerable<IMigration> _migrations;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public string CurrentDatabasePath { get; private set; }
        [CanBeNull] public LocalDbContext Context { get; private set; }

        public DatabaseManager(IEnumerable<IMigration> migrations) {
            _migrations = migrations;
        }

        private bool CheckFileExist([NotNull] string path) {
            return File.Exists(path) && path.EndsWith(LocalDbContext.DatabaseExtension);
        }

        private SQLiteConnection CreateConnection(string path) {
            var dbConnection = SQLiteFactory.Instance.CreateConnection();
            if (dbConnection == null) {
                Logger.Log(LogLevel.Error, "Cannot create sqlite connection");
                throw new SqliteInternalException();
            }

            dbConnection.ConnectionString = new SQLiteConnectionStringBuilder {
                    DataSource = path,
                    ForeignKeys = true,
                    Version = 3
                }
                .ConnectionString;
            return (SQLiteConnection) dbConnection;
        }

        public void CreateAndConnectNewDatabase(string path) {
            DisposeConnection();
            LocalDbContext context;
            try {
                context = new LocalDbContext(CreateConnection(path));
                context.Database.Initialize(false);
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Info, "Failed to connect to database file: {0}", path);
                Logger.Log(LogLevel.Error, e);
                throw;
            }

            this.Context = context;
            this.CurrentDatabasePath = path;
        }

        public async Task Connect([NotNull] string path) {
            var isExist = CheckFileExist(path);
            if (!isExist) {
                Logger.Log(LogLevel.Info, "Failed to connect to non-database file: {0}", path);
                throw new FileNotFoundException(path);
            }

            DisposeConnection();
            LocalDbContext context;
            try {
                var sqLiteConnection = CreateConnection(path);
                context = new LocalDbContext(sqLiteConnection);
                var exists = context.Database.Exists();
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Info, "Failed to connect to database file: {0}", path);
                Logger.Log(LogLevel.Error, e);
                throw;
            }

            await Migrate(context);
            this.Context = context;
            this.CurrentDatabasePath = path;
        }

        private async Task Migrate([NotNull] LocalDbContext context) {
            var currentVersion = long.Parse(Resources.DatabaseVersion);
            var databaseVersion = context.GetDatabaseVersion();
            if (databaseVersion == currentVersion) {
                Logger.Log(LogLevel.Info, "Database is actual, no migrations needed");
                return;
            }

            Logger.Log(LogLevel.Info, $"Starting update from {databaseVersion} to {currentVersion}");
            while (currentVersion != databaseVersion) {
                var migration = _migrations.FirstOrDefault(m => m.From == databaseVersion);
                if (migration == null) {
                    var message = $"Cannot find suitable migration: from {databaseVersion} to {currentVersion}";
                    Logger.Log(LogLevel.Error, message);
                    throw new Exception(message);
                }

                await migration.Migrate(context);
                databaseVersion = context.GetDatabaseVersion();
            }

            Logger.Log(LogLevel.Info, "Database is updated");
        }

        public void Backup(string path) {
            if (this.Context == null) {
                return;
            }

            using (var target = CreateConnection(path))
            using (var source = CreateConnection(this.CurrentDatabasePath)) {
                source.Open();
                target.Open();
                source.BackupDatabase(target, "main", "main",
                    -1, null, 0);
            }
        }

        private void DisposeConnection() {
            if (this.Context == null) {
                return;
            }

            try {
                this.Context?.Database.Connection.Close();
                this.Context?.Dispose();
            }
            catch (Exception e) {
                Logger.Log(LogLevel.Error, "Cannot dispose context");
                Logger.Log(LogLevel.Error, e);
            }

            this.Context = null;
        }

        public void Dispose() {
            DisposeConnection();
        }
    }
}
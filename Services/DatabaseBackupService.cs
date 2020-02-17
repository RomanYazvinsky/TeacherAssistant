using System;
using System.IO;
using TeacherAssistant.Database;
using TeacherAssistant.Properties;

namespace TeacherAssistant {
    public class DatabaseBackupService {
        private readonly DatabaseManager _databaseManager;
        private readonly Random _random = new Random();
        public DatabaseBackupService(DatabaseManager databaseManager) {
            _databaseManager = databaseManager;
            CreatePathIfNotExist();
        }

        private string GenerateDatabaseBackupName() {
            return $"Database Id {_random.Next(99)} {DateTime.Now:dd-MM-yyyy hh-mm-ss}";
        }

        private void CreatePathIfNotExist() {
            if (string.IsNullOrWhiteSpace(Resources.DatabaseBackupDir)) {
                throw new Exception("Database backup path is not configured");
            }
            var exists = Directory.Exists(Resources.DatabaseBackupDir);
            if (!exists) {
                Directory.CreateDirectory(Resources.DatabaseBackupDir);
            }
        }
        public void BackupDatabase() {
            _databaseManager.Backup(Path.Combine(Resources.DatabaseBackupDir, GenerateDatabaseBackupName() + LocalDbContext.DatabaseExtension));
        }
    }
}
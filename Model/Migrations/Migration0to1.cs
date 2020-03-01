using System;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using TeacherAssistant.Database;
using TeacherAssistant.Models;

namespace TeacherAssistant.Migrations {
    public class Migration0to1 : IMigration {
        private const string RemoveUncascadedStudentLessons = @"
            delete from STUDENT_LESSON where not exists (select id from STUDENT where id == STUDENT_LESSON.student_id);
            delete from STUDENT_LESSON where not exists (select id from LESSON where id == STUDENT_LESSON.lesson_id);
        ";

        private const string MpegStartMetadata = "ID3"; // ID3v2 start metadata
        private const string WavStartMetadata = "RIFF"; // wav start metadata
        public async Task Migrate(LocalDbContext context) {
            var count = await context.LessonTypes.CountAsync();
            var isLessonTypesMissed = count < 5 && count >= 3;
            if (isLessonTypesMissed) {
                context.LessonTypes.Add(new LessonTypeEntity {Name = "Аттестация"});
                context.LessonTypes.Add(new LessonTypeEntity {Name = "Экзамен"});
                await context.SaveChangesAsync();
            }

            await context.Database.ExecuteSqlCommandAsync(RemoveUncascadedStudentLessons);
            await context.Database.ExecuteSqlCommandAsync("PRAGMA foreign_keys=off;");
            await context.Database.ExecuteSqlCommandAsync
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

            await context.Database.ExecuteSqlCommandAsync(@"
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

            await context.Database.ExecuteSqlCommandAsync("PRAGMA foreign_keys=on;");
            await TrySetAudioDiscriminator(context);
            await context.Database.ExecuteSqlCommandAsync($"PRAGMA user_version={this.To};");
        }

        
        private async Task TrySetAudioDiscriminator(LocalDbContext context) {
            var alarms = await context.Alarms.ToListAsync();
            foreach (var alarmEntity in alarms) {
                var sound = alarmEntity.Sound;
                if (sound == null || sound.Length == 0 || !string.IsNullOrWhiteSpace(alarmEntity.Discriminator)) {
                    continue;
                }

                var metadataStart = new byte[20];
                Array.Copy(sound, metadataStart, 20);
                var metadataAsString = Encoding.ASCII.GetString(metadataStart);
                if (metadataAsString.StartsWith(MpegStartMetadata)) {
                    alarmEntity.Discriminator = ".mp3";
                }

                if (metadataAsString.StartsWith(WavStartMetadata)) {
                    alarmEntity.Discriminator = ".wav";
                }
            }

            await context.SaveChangesAsync();
        }
        
        public int From { get; } = 0;
        public int To { get; } = 1;
    }
}
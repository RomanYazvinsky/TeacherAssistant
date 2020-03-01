using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using TeacherAssistant.Models;
using TeacherAssistant.Models.Notes;

namespace TeacherAssistant.Database.ModelConfiguration {
    public static class ModelConfiguration {
        public static void Configure(DbModelBuilder modelBuilder) {
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
    }
}
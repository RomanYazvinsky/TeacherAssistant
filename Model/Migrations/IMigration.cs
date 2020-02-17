using System.Threading.Tasks;
using JetBrains.Annotations;
using TeacherAssistant.Dao;
using TeacherAssistant.Database;

namespace TeacherAssistant.Migrations {
    public interface IMigration {
        Task Migrate([NotNull] LocalDbContext context);
        int From { get; }
        int To { get; }
    }
}
using System.Collections.Generic;
using Grace.DependencyInjection;
using TeacherAssistant.Dao;
using TeacherAssistant.Migrations;

namespace TeacherAssistant.Database
{
    public class DatabaseModule : IConfigurationModule
    {
        public void Configure(IExportRegistrationBlock registrationBlock)
        {
            registrationBlock.ExportAssembly(typeof(IMigration).Assembly)
                .Where(y => typeof(IMigration).IsAssignableFrom(y))
                .ByInterface<IMigration>()
                .Lifestyle.Singleton();
            registrationBlock.Export<DatabaseManager>().WithCtorCollectionParam<IEnumerable<IMigration>, IMigration>()
                .Lifestyle.Singleton();
            registrationBlock.ExportFactory<DatabaseManager, LocalDbContext>(c => c.Context).ExternallyOwned();
        }
    }
}

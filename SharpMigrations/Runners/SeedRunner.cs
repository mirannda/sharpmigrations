using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SharpData;

namespace SharpMigrations.Runners {
    public class SeedRunner : ISeedRunner {
        private IDataClient _dataClient;
        private Assembly _targetAssembly;

        public static ILogger Logger { get; set; } = SharpMigrationsLogging.CreateLogger<SeedRunner>();

        public SeedRunner(IDataClient dataClient, Assembly targetAssembly) {
            _dataClient = dataClient;
            _targetAssembly = targetAssembly;
        }

        public void Run(string seedName, string param = null, string migrationGroup = null) {
            var seedType = MigrationFinder.FindSeed(_targetAssembly, seedName);
            Logger.LogInformation("Starting seed migration");
            Logger.LogInformation(String.Format("Applying Seed -> [{0}]", seedName));

            var migration = (SeedMigration) Activator.CreateInstance(seedType);
            migration.SetDataClient(_dataClient);
            migration.Up(param);
            _dataClient.Commit();
        }
    }
}
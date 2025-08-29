using DbUp;
using DbUp.Engine;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    public static class DatabaseMigration
    {
        public static bool RunMigrations(string connectionString, ILogger logger)
        {
            logger.LogInformation("Starting database migrations...");

            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            UpgradeEngine upgrader = DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigration).Assembly)
                .LogTo(new DbUpLogger(logger))
                .WithTransaction()
                .Build();

            DatabaseUpgradeResult result = upgrader.PerformUpgrade();

            if (result.Successful)
            {
                logger.LogInformation("Database migrations completed successfully");
                return true;
            }
            else
            {
                logger.LogError("Database migrations failed: {Error}", result.Error);
                return false;
            }
        }

        private class DbUpLogger : DbUp.Engine.Output.IUpgradeLog
        {
            private readonly ILogger _logger;

            public DbUpLogger(ILogger logger)
            {
                _logger = logger;
            }

            public void WriteInformation(string message)
            {
                _logger.LogInformation(message);
            }

            public void WriteInformation(string message, params object[] args)
            {
                _logger.LogInformation(message, args);
            }

            public void WriteError(string message)
            {
                _logger.LogError(message);
            }

            public void WriteError(string message, params object[] args)
            {
                _logger.LogError(message, args);
            }

            public void WriteWarning(string message)
            {
                _logger.LogWarning(message);
            }

            public void WriteWarning(string message, params object[] args)
            {
                _logger.LogWarning(message, args);
            }
        }
    }
}

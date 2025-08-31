using DbUp;
using DbUp.Engine;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    public static class DatabaseMigrator
    {
        public static bool RunMigrations(string connectionString, ILogger logger)
        {
            logger.LogInformation("Starting database migrations...");

            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            UpgradeEngine upgrader = DeployChanges.To
                .PostgresqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
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

            public void LogTrace(string message, params object[] args)
            {
                _logger.LogTrace(message, args);
            }

            public void LogDebug(string message, params object[] args)
            {
                _logger.LogDebug(message, args);
            }

            public void LogInformation(string message, params object[] args)
            {
                _logger.LogInformation(message, args);
            }

            public void LogWarning(string message, params object[] args)
            {
                _logger.LogWarning(message, args);
            }

            public void LogError(string message, params object[] args)
            {
                _logger.LogError(message, args);
            }

            public void LogError(Exception exception, string message, params object[] args)
            {
                _logger.LogError(exception, message, args);
            }
        }
    }
}

using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    public static class TestDatabaseMigrator
    {
        public static bool EnsureDatabaseExists(string connectionString, ILogger logger)
        {
            logger.LogInformation("Ensuring test database exists...");

            try
            {
                EnsureDatabase.For.PostgresqlDatabase(connectionString);
                logger.LogInformation("Test database ensured successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure test database exists");
                return false;
            }
        }

        public static bool RunMigrations(string connectionString, ILogger logger)
        {
            logger.LogInformation("Running test database migrations...");

            try
            {
                UpgradeEngine upgrader = DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(typeof(TestDatabaseMigrator).Assembly)
                    .LogTo(new TestDbUpLogger(logger))
                    .WithTransaction()
                    .Build();

                DatabaseUpgradeResult result = upgrader.PerformUpgrade();

                if (result.Successful)
                {
                    logger.LogInformation("Test database migrations completed successfully");
                    return true;
                }
                else
                {
                    logger.LogError("Test database migrations failed: {Error}", result.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run test database migrations");
                return false;
            }
        }

        public static bool CleanupDatabase(string connectionString, ILogger logger)
        {
            logger.LogInformation("Cleaning up test database...");

            try
            {
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();
                
                // Drop all tables to ensure clean state
                using NpgsqlCommand command = new NpgsqlCommand(@"
                    DROP TABLE IF EXISTS knowledge_chunks CASCADE;
                    DROP TABLE IF EXISTS knowledge_sections CASCADE;
                    DROP TABLE IF EXISTS knowledge_files CASCADE;", connection);
                
                command.ExecuteNonQuery();
                
                logger.LogInformation("Test database cleanup completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup test database");
                return false;
            }
        }

        public static bool ResetDatabase(string connectionString, ILogger logger)
        {
            logger.LogInformation("Resetting test database...");

            // First cleanup
            if (!CleanupDatabase(connectionString, logger))
            {
                return false;
            }

            // Then run migrations
            return RunMigrations(connectionString, logger);
        }

        private class TestDbUpLogger : DbUp.Engine.Output.IUpgradeLog
        {
            private readonly ILogger _logger;

            public TestDbUpLogger(ILogger logger)
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

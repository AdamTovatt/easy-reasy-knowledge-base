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
                    .WithScriptsEmbeddedInAssembly(typeof(EasyReasy.KnowledgeBase.Storage.Postgres.PostgresFileStore).Assembly)
                    .LogTo(new TestDbUpLogger(logger))
                    .WithoutTransaction()
                    .Build();

                DatabaseUpgradeResult result = upgrader.PerformUpgrade();

                if (result.Successful)
                {
                    logger.LogInformation("Test database migrations completed successfully");

                    // Verify tables were actually created
                    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    using NpgsqlCommand command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE 'knowledge_%'", connection);
                    using NpgsqlDataReader reader = command.ExecuteReader();
                    List<string> tables = new List<string>();
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }

                    logger.LogInformation("Tables found after migration: {Tables}", string.Join(", ", tables));

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

                // Check if tables exist before trying to delete from them
                using NpgsqlCommand checkCommand = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name IN ('knowledge_chunk', 'knowledge_section', 'knowledge_file')", connection);

                int tableCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (tableCount > 0)
                {
                    // Delete all data but keep the schema
                    // Delete in order to respect foreign key constraints
                    using NpgsqlCommand command = new NpgsqlCommand(@"
                                    DELETE FROM knowledge_chunk;
            DELETE FROM knowledge_section;
            DELETE FROM knowledge_file;", connection);

                    int rowsAffected = command.ExecuteNonQuery();
                    logger.LogInformation("Test database cleanup completed successfully. Rows affected: {RowsAffected}", rowsAffected);
                }
                else
                {
                    logger.LogInformation("No tables exist to cleanup");
                }

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

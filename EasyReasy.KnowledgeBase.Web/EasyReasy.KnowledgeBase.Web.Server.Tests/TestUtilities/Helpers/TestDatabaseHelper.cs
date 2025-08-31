using EasyReasy.EnvironmentVariables;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers
{
    /// <summary>
    /// Helper class for managing test database setup and cleanup.
    /// </summary>
    public static class TestDatabaseHelper
    {
        private static string? _connectionString = null;

        /// <summary>
        /// Gets the database connection string from environment variables.
        /// </summary>
        /// <returns>The PostgreSQL connection string.</returns>
        public static string GetConnectionString()
        {
            if (_connectionString == null)
            {
                _connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            }

            return _connectionString;
        }

        /// <summary>
        /// Creates a logger for test output.
        /// </summary>
        /// <typeparam name="T">The type to create a logger for.</typeparam>
        /// <returns>A configured logger.</returns>
        public static ILogger CreateLogger<T>() where T : class
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<T>();
        }

        /// <summary>
        /// Sets up the test database by running migrations.
        /// </summary>
        /// <typeparam name="T">The test class type for logging.</typeparam>
        /// <returns>True if setup was successful, false otherwise.</returns>
        public static bool SetupDatabase<T>() where T : class
        {
            ILogger logger = CreateLogger<T>();
            return RunMigrations(GetConnectionString(), logger);
        }

        /// <summary>
        /// Cleans up the test database by dropping all tables.
        /// </summary>
        /// <typeparam name="T">The test class type for logging.</typeparam>
        /// <returns>True if cleanup was successful, false otherwise.</returns>
        public static bool CleanupDatabase<T>() where T : class
        {
            ILogger logger = CreateLogger<T>();
            return DropAllTables(GetConnectionString(), logger);
        }

        /// <summary>
        /// Runs database migrations using the main project's DatabaseMigrator.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="logger">The logger to use for output.</param>
        /// <returns>True if migrations were successful, false otherwise.</returns>
        private static bool RunMigrations(string connectionString, ILogger logger)
        {
            try
            {
                return DatabaseMigrator.RunMigrations(connectionString, logger);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to run database migrations");
                return false;
            }
        }

        /// <summary>
        /// Drops all tables in the test database.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="logger">The logger to use for output.</param>
        /// <returns>True if cleanup was successful, false otherwise.</returns>
        private static bool DropAllTables(string connectionString, ILogger logger)
        {
            try
            {
                using IDbConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                // Drop tables in correct order (respecting foreign key constraints)
                string[] dropCommands = {
                "DROP TABLE IF EXISTS user_role CASCADE",
                "DROP TABLE IF EXISTS \"user\" CASCADE",
                "DROP FUNCTION IF EXISTS update_updated_at_column() CASCADE"
            };

                foreach (string command in dropCommands)
                {
                    using IDbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                }

                logger.LogInformation("Database cleanup completed successfully.");
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to cleanup database");
                return false;
            }
        }
    }
}

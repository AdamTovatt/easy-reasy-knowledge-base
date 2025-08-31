using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers
{
    /// <summary>
    /// Handles database migration and cleanup for web server tests.
    /// </summary>
    public static class TestDatabaseMigrator
    {
        /// <summary>
        /// Runs database migrations using the main project's DatabaseMigrator.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="logger">The logger to use for output.</param>
        /// <returns>True if migrations were successful, false otherwise.</returns>
        public static bool RunMigrations(string connectionString, ILogger logger)
        {
            try
            {
                logger.LogInformation("Running web server test database migrations...");
                return DatabaseMigrator.RunMigrations(connectionString, logger);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to run database migrations");
                return false;
            }
        }

        /// <summary>
        /// Cleans up a specific table by deleting all data from it.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="tableName">The name of the table to clean up.</param>
        /// <param name="logger">The logger to use for output.</param>
        /// <returns>True if cleanup was successful, false otherwise.</returns>
        public static bool CleanupTable(string connectionString, string tableName, ILogger logger)
        {
            try
            {
                using IDbConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();

                string deleteCommand = $"DELETE FROM {tableName}";
                using IDbCommand cmd = connection.CreateCommand();
                cmd.CommandText = deleteCommand;
                int rowsAffected = cmd.ExecuteNonQuery();

                logger.LogDebug("Deleted {RowCount} rows from table: {TableName}", rowsAffected, tableName);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to cleanup table {TableName}", tableName);
                return false;
            }
        }
    }
}

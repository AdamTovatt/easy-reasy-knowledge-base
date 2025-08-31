using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Database;
using Microsoft.Extensions.Logging;
using System.Data;
using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses
{
    /// <summary>
    /// Base class for database integration tests that provides database setup and cleanup.
    /// </summary>
    public abstract class DatabaseTestBase : GeneralTestBase
    {
        protected static IDbConnectionFactory _connectionFactory = null!;
        protected static ILogger _logger = null!;

        /// <summary>
        /// Initializes the test environment with database connections.
        /// </summary>
        protected static void InitializeDatabaseTestEnvironment()
        {
            try
            {
                // Load environment variables
                LoadTestEnvironmentVariables();

                // Validate environment variables
                TestEnvironmentVariables.PostgresConnectionString.GetValue();

                // Create connection factory
                string connectionString = TestDatabaseHelper.GetConnectionString();
                _connectionFactory = new PostgresConnectionFactory(connectionString);

                // Create logger
                _logger = TestDatabaseHelper.CreateLogger<DatabaseTestBase>();

                // Set up database schema
                if (!TestDatabaseHelper.SetupDatabase<DatabaseTestBase>())
                {
                    throw new InvalidOperationException("Failed to set up test database");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize database test environment: {exception.Message}");
                Assert.Inconclusive("Failed to initialize database test environment for integration tests.");
            }
        }

        /// <summary>
        /// Cleans up the test environment and disposes of resources.
        /// </summary>
        protected static void CleanupDatabaseTestEnvironment()
        {
            _connectionFactory = null!;
            _logger = null!;

            // Clean up the test database
            TestDatabaseHelper.CleanupDatabase<DatabaseTestBase>();
        }

        /// <summary>
        /// Sets up the database for each individual test.
        /// </summary>
        protected static void SetupTestDatabase()
        {
            // Clean up any existing data
            CleanupTestData();
        }

        /// <summary>
        /// Cleans up test data after each test.
        /// </summary>
        protected static void CleanupTestData()
        {
            try
            {
                using IDbConnection connection = _connectionFactory.CreateOpenConnectionAsync().Result;

                // Delete data in correct order (respecting foreign key constraints)
                string[] cleanupCommands = {
                "DELETE FROM user_role",
                "DELETE FROM \"user\""
            };

                foreach (string command in cleanupCommands)
                {
                    using IDbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = command;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to cleanup test data");
            }
        }
    }
}

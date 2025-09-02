using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Database;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;
using Microsoft.Extensions.Logging;
using System.Data;

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
                EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(TestEnvironmentVariables));

                // Create connection factory
                string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
                _connectionFactory = new PostgresConnectionFactory(connectionString);

                // Create logger
                using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                _logger = loggerFactory.CreateLogger<DatabaseTestBase>();

                // Set up database schema
                if (!TestDatabaseMigrator.RunMigrations(connectionString, _logger))
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
    }
}

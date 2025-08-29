using EasyReasy.EnvironmentVariables;
using Microsoft.Extensions.Logging;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    public static class TestDatabaseHelper
    {
        private static string? _connectionString = null;

        public static string GetConnectionString()
        {
            if (_connectionString == null)
            {
                _connectionString = PostgresTestEnvironmentVariables.PostgresConnectionString.GetValue();
            }

            return _connectionString;
        }

        public static ILogger CreateLogger<T>() where T : class
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<T>();
        }

        public static bool SetupDatabase<T>() where T : class
        {
            ILogger logger = CreateLogger<T>();
            return TestDatabaseMigrator.ResetDatabase(GetConnectionString(), logger);
        }

        public static bool CleanupDatabase<T>() where T : class
        {
            ILogger logger = CreateLogger<T>();
            return TestDatabaseMigrator.CleanupDatabase(GetConnectionString(), logger);
        }
    }
}

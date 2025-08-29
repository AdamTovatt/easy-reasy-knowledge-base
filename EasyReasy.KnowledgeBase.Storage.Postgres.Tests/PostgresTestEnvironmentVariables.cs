using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    /// <summary>
    /// Environment variable configuration for PostgreSQL integration tests.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class PostgresTestEnvironmentVariables
    {
        /// <summary>
        /// The full PostgreSQL connection string for testing.
        /// </summary>
        [EnvironmentVariableName(minLength: 10)]
        public static readonly VariableName PostgresConnectionString = new VariableName("POSTGRES_CONNECTION_STRING");
    }
}

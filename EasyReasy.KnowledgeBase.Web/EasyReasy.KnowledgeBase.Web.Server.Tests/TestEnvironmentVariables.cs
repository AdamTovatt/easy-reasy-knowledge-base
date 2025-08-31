using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests
{
    /// <summary>
    /// Environment variable configuration for web server integration tests.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class TestEnvironmentVariables
    {
        /// <summary>
        /// The full PostgreSQL connection string for testing.
        /// </summary>
        [EnvironmentVariableName(minLength: 10)]
        public static readonly VariableName PostgresConnectionString = new VariableName("POSTGRES_CONNECTION_STRING");
    }
}

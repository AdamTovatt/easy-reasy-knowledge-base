using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    /// <summary>
    /// Assembly-level initialization for PostgreSQL integration tests.
    /// Loads environment variables once for the entire test assembly.
    /// </summary>
    [TestClass]
    public static class TestAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            // Load environment variables from test configuration file
            EnvironmentVariableHelper.LoadVariablesFromFile("..\\..\\TestEnvironmentVariables.txt");
            EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(PostgresTestEnvironmentVariables));

            Console.WriteLine("PostgreSQL test environment variables loaded successfully.");
        }
    }
}

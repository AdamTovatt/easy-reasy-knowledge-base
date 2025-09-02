using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Controllers
{
    /// <summary>
    /// Environment variable configuration for FileStorageController integration tests.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class FileStorageControllerIntegrationTestEnvironmentVariables
    {
        /// <summary>
        /// Base path for file storage during integration tests.
        /// </summary>
        [EnvironmentVariableName(minLength: 1)]
        public static readonly VariableName FileStorageBasePath = new VariableName("FILE_STORAGE_BASE_PATH");

        /// <summary>
        /// Maximum file size in bytes for uploads during integration tests.
        /// </summary>
        [EnvironmentVariableName(minLength: 1)]
        public static readonly VariableName MaxFileSizeBytes = new VariableName("MAX_FILE_SIZE_BYTES");

        /// <summary>
        /// PostgreSQL connection string for database operations during integration tests.
        /// </summary>
        [EnvironmentVariableName(minLength: 10)]
        public static readonly VariableName PostgresConnectionString = new VariableName("POSTGRES_CONNECTION_STRING");

        /// <summary>
        /// JWT signing secret for authentication during integration tests.
        /// </summary>
        [EnvironmentVariableName(minLength: 32)]
        public static readonly VariableName JwtSigningSecret = new VariableName("JWT_SIGNING_SECRET");
    }
}

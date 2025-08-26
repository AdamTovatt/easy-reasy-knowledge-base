using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server
{
    /// <summary>
    /// Environment variable configuration for the Knowledge Base Web Server.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class EnvironmentVariables
    {
        /// <summary>
        /// Database connection string for the knowledge base.
        /// </summary>
        [EnvironmentVariableName(minLength: 5)]
        public static readonly VariableName DatabaseConnectionString = new VariableName("DATABASE_CONNECTION_STRING");

        /// <summary>
        /// Ollama server URLs (e.g., OLLAMA_SERVER_URL_1, OLLAMA_SERVER_URL_2).
        /// </summary>
        [EnvironmentVariableNameRange(minCount: 1)]
        public static readonly VariableNameRange OllamaServerUrls = new VariableNameRange("OLLAMA_SERVER_URL");

        /// <summary>
        /// Ollama server API keys (e.g., OLLAMA_SERVER_API_KEY_1, OLLAMA_SERVER_API_KEY_2).
        /// </summary>
        [EnvironmentVariableNameRange(minCount: 1)]
        public static readonly VariableNameRange OllamaServerApiKeys = new VariableNameRange("OLLAMA_SERVER_API_KEY");

        /// <summary>
        /// JWT signing secret for authentication.
        /// </summary>
        [EnvironmentVariableName(minLength: 32)]
        public static readonly VariableName JwtSigningSecret = new VariableName("JWT_SIGNING_SECRET");

        /// <summary>
        /// Logging level configuration.
        /// </summary>
        [EnvironmentVariableName]
        public static readonly VariableName LogLevel = new VariableName("LOG_LEVEL");
    }
}

using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    /// <summary>
    /// Environment variable configuration for the Knowledge Base Web Server.
    /// </summary>
    [EnvironmentVariableNameContainer]
    public static class EnvironmentVariable
    {
        /// <summary>
        /// Database connection string for the knowledge base.
        /// </summary>
        [EnvironmentVariableName(minLength: 5)]
        public static readonly VariableName DatabaseConnectionString = new VariableName("DATABASE_CONNECTION_STRING");

        /// <summary>
        /// Path where vector store data will be saved.
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName VectorStoreSavePath = new VariableName("VECTOR_STORE_SAVE_PATH");

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
        /// Ollama embedding model name (e.g., nomic-embed-text).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaEmbeddingModelName = new VariableName("OLLAMA_EMBEDDING_MODEL_NAME");

        /// <summary>
        /// Ollama small text completion model name (e.g., llama3.1:3b, llama3.2:3b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaSmallTextCompletionModelName = new VariableName("OLLAMA_SMALL_TEXT_COMPLETION_MODEL_NAME");

        /// <summary>
        /// Ollama large text completion model name (e.g., llama3.1:8b, llama3.2:8b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaLargeTextCompletionModelName = new VariableName("OLLAMA_LARGE_TEXT_COMPLETION_MODEL_NAME");

        /// <summary>
        /// Ollama reasoning text completion model name (e.g., llama3.1:70b, llama3.2:70b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaReasoningTextCompletionModelName = new VariableName("OLLAMA_REASONING_TEXT_COMPLETION_MODEL_NAME");

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

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
        /// Postgres database connection string.
        /// </summary>
        [EnvironmentVariableName(minLength: 5)]
        public static readonly VariableName PostgresConnectionString = new("POSTGRES_CONNECTION_STRING");

        /// <summary>
        /// Path where vector store data will be saved.
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName VectorStoreSavePath = new("VECTOR_STORE_SAVE_PATH");

        /// <summary>
        /// Ollama server URLs (e.g., OLLAMA_SERVER_URL_1, OLLAMA_SERVER_URL_2).
        /// </summary>
        [EnvironmentVariableNameRange(minCount: 1)]
        public static readonly VariableNameRange OllamaServerUrls = new("OLLAMA_SERVER_URL");

        /// <summary>
        /// Ollama server API keys (e.g., OLLAMA_SERVER_API_KEY_1, OLLAMA_SERVER_API_KEY_2).
        /// </summary>
        [EnvironmentVariableNameRange(minCount: 1)]
        public static readonly VariableNameRange OllamaServerApiKeys = new("OLLAMA_SERVER_API_KEY");

        /// <summary>
        /// Ollama embedding model name (e.g., nomic-embed-text).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaEmbeddingModelName = new("OLLAMA_EMBEDDING_MODEL_NAME");

        /// <summary>
        /// Ollama small text completion model name (e.g., llama3.1:3b, llama3.2:3b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaSmallTextCompletionModelName = new("OLLAMA_SMALL_TEXT_COMPLETION_MODEL_NAME");

        /// <summary>
        /// Ollama large text completion model name (e.g., llama3.1:8b, llama3.2:8b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaLargeTextCompletionModelName = new("OLLAMA_LARGE_TEXT_COMPLETION_MODEL_NAME");

        /// <summary>
        /// Ollama reasoning text completion model name (e.g., llama3.1:70b, llama3.2:70b).
        /// </summary>
        [EnvironmentVariableName(minLength: 3)]
        public static readonly VariableName OllamaReasoningTextCompletionModelName = new("OLLAMA_REASONING_TEXT_COMPLETION_MODEL_NAME");

        /// <summary>
        /// JWT signing secret for authentication.
        /// </summary>
        [EnvironmentVariableName(minLength: 32)]
        public static readonly VariableName JwtSigningSecret = new("JWT_SIGNING_SECRET");

        /// <summary>
        /// Base path for file storage operations.
        /// </summary>
        [EnvironmentVariableName(minLength: 1)]
        public static readonly VariableName FileStorageBasePath = new("FILE_STORAGE_BASE_PATH");

        /// <summary>
        /// Maximum file size in bytes that can be uploaded.
        /// </summary>
        [EnvironmentVariableName(minLength: 1)]
        public static readonly VariableName MaxFileSizeBytes = new("MAX_FILE_SIZE_BYTES");


        /// <summary>
        /// Logging level configuration.
        /// </summary>
        [EnvironmentVariableName]
        public static readonly VariableName LogLevel = new("LOG_LEVEL");
    }
}

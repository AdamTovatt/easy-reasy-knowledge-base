using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.OllamaGeneration;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Ai
{
    /// <summary>
    /// Provider for the embedding service with error handling and lazy loading.
    /// </summary>
    public class EmbeddingServiceProvider : AiServiceProviderBase<IEmbeddingService>
    {
        public EmbeddingServiceProvider(ILogger<EmbeddingServiceProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// Gets the name of the service for health reporting.
        /// </summary>
        public override string ServiceName => "Embedding Service";

        protected override async Task<IEmbeddingService> CreateServiceAsync()
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();
            string modelName = EnvironmentVariable.OllamaEmbeddingModelName.GetValue();

            return await EasyReasyOllamaEmbeddingService.CreateAsync(baseUrl, apiKey, modelName);
        }
    }
}

using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.OllamaGeneration;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    /// <summary>
    /// Provider for the embedding service with error handling and lazy loading.
    /// </summary>
    public class EmbeddingServiceProvider : AiServiceProviderBase<IEmbeddingService>
    {
        public EmbeddingServiceProvider(ILogger<EmbeddingServiceProvider> logger) : base(logger)
        {
        }

        protected override async Task<IEmbeddingService> CreateServiceAsync()
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();
            string modelName = EnvironmentVariable.OllamaEmbeddingModelName.GetValue();

            return await EasyReasyOllamaEmbeddingService.CreateAsync(baseUrl, apiKey, modelName);
        }
    }
}

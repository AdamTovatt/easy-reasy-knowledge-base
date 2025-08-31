using EasyReasy.EnvironmentVariables;
using EasyReasy.Ollama.Client;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    /// <summary>
    /// Provider for the Ollama client with error handling and lazy loading.
    /// </summary>
    public class OllamaClientProvider : AiServiceProviderBase<OllamaClient>
    {
        public OllamaClientProvider(ILogger<OllamaClientProvider> logger) : base(logger)
        {
        }

        protected override async Task<OllamaClient> CreateServiceAsync()
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();

            return await OllamaClient.CreateAuthorizedAsync(new HttpClient { BaseAddress = new Uri(baseUrl) }, apiKey);
        }
    }
}

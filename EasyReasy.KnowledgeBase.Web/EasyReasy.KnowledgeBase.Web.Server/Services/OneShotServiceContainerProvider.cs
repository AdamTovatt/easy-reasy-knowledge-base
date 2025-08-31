using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.OllamaGeneration;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    /// <summary>
    /// Provider for the one-shot service container with error handling and lazy loading.
    /// </summary>
    public class OneShotServiceContainerProvider : AiServiceProviderBase<AiServiceContainer<IOneShotService>>
    {
        public OneShotServiceContainerProvider(ILogger<OneShotServiceContainerProvider> logger) : base(logger)
        {
        }

        protected override async Task<AiServiceContainer<IOneShotService>> CreateServiceAsync()
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();

            string smallModelName = EnvironmentVariable.OllamaSmallTextCompletionModelName.GetValue();
            string largeModelName = EnvironmentVariable.OllamaLargeTextCompletionModelName.GetValue();
            string reasoningModelName = EnvironmentVariable.OllamaReasoningTextCompletionModelName.GetValue();

            IOneShotService smallOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, smallModelName);
            IOneShotService largeOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, largeModelName);
            IOneShotService reasoningOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, reasoningModelName);

            return new AiServiceContainer<IOneShotService>(smallOneShotService, largeOneShotService, reasoningOneShotService);
        }
    }
}

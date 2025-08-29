using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.OllamaGeneration;
using EasyReasy.KnowledgeBase.Web.Server.Services;
using EasyReasy.Ollama.Client;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    /// <summary>
    /// Configures AI services for the application, including embedding and one-shot services.
    /// </summary>
    public static class AiServiceConfigurator
    {
        /// <summary>
        /// Configures the embedding service for the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static async Task ConfigureEmbeddingServiceAsync(IServiceCollection services)
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();
            string modelName = EnvironmentVariable.OllamaEmbeddingModelName.GetValue();

            IEmbeddingService embeddingService = await EasyReasyOllamaEmbeddingService.CreateAsync(baseUrl, apiKey, modelName);
            services.AddSingleton(embeddingService);
        }

        /// <summary>
        /// Configures one-shot services for different model sizes.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static async Task ConfigureOneShotServicesAsync(IServiceCollection services)
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();

            string smallModelName = EnvironmentVariable.OllamaSmallTextCompletionModelName.GetValue();
            string largeModelName = EnvironmentVariable.OllamaLargeTextCompletionModelName.GetValue();
            string reasoningModelName = EnvironmentVariable.OllamaReasoningTextCompletionModelName.GetValue();

            IOneShotService smallOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, smallModelName);
            IOneShotService largeOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, largeModelName);
            IOneShotService reasoningOneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, reasoningModelName);

            // Create the AI service container
            AiServiceContainer<IOneShotService> oneShotServiceContainer = new AiServiceContainer<IOneShotService>(
                smallOneShotService, largeOneShotService, reasoningOneShotService);

            services.AddSingleton(oneShotServiceContainer);
        }

        /// <summary>
        /// Configures chat clients for different model sizes.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static async Task ConfigureChatClientsAsync(IServiceCollection services)
        {
            string baseUrl = EnvironmentVariable.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariable.OllamaServerApiKeys.GetAllValues().First();

            // Create a single authenticated client - the model is specified in each chat request
            OllamaClient ollamaClient = await OllamaClient.CreateAuthorizedAsync(new HttpClient { BaseAddress = new Uri(baseUrl) }, apiKey);

            services.AddSingleton(ollamaClient);
        }

        /// <summary>
        /// Configures all AI services for the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static async Task ConfigureAllAiServicesAsync(IServiceCollection services)
        {
            await ConfigureEmbeddingServiceAsync(services);
            await ConfigureOneShotServicesAsync(services);
            await ConfigureChatClientsAsync(services);
        }
    }
}

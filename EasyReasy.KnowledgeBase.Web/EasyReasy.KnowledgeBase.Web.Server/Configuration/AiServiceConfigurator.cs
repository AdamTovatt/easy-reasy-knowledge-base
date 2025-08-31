using EasyReasy.KnowledgeBase.Web.Server.Services.Ai;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    /// <summary>
    /// Configures AI services for the application, including embedding and one-shot services.
    /// </summary>
    public static class AiServiceConfigurator
    {
        /// <summary>
        /// Configures all AI services for the application using providers that handle errors gracefully.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public static void ConfigureAllAiServices(IServiceCollection services)
        {
            // Register providers (these won't fail startup)
            services.AddSingleton<EmbeddingServiceProvider>();
            services.AddSingleton<OneShotServiceContainerProvider>();
            services.AddSingleton<OllamaClientProvider>();
        }

        /// <summary>
        /// Preloads all AI services using the provided service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for resolving services.</param>
        public static async Task PreloadAllAiServicesAsync(IServiceProvider serviceProvider)
        {
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Preload embedding service
            EmbeddingServiceProvider embeddingProvider = serviceProvider.GetRequiredService<EmbeddingServiceProvider>();
            await embeddingProvider.GetServiceAsync(); // Preload attempt - let provider handle errors

            // Preload one-shot service container
            OneShotServiceContainerProvider oneShotProvider = serviceProvider.GetRequiredService<OneShotServiceContainerProvider>();
            await oneShotProvider.GetServiceAsync(); // Preload attempt - let provider handle errors

            // Preload Ollama client
            OllamaClientProvider ollamaClientProvider = serviceProvider.GetRequiredService<OllamaClientProvider>();
            await ollamaClientProvider.GetServiceAsync(); // Preload attempt - let provider handle errors
        }
    }
}

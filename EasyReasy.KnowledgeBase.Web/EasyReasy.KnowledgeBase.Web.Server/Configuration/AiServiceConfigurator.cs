using EasyReasy.KnowledgeBase.Web.Server.Services;

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
        public static async Task ConfigureAllAiServicesAsync(IServiceCollection services)
        {
            // Register providers (these won't fail startup)
            services.AddSingleton<EmbeddingServiceProvider>();
            services.AddSingleton<OneShotServiceContainerProvider>();
            services.AddSingleton<OllamaClientProvider>();

            // Try to preload services (but don't fail if they can't connect)
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Preload embedding service
            try
            {
                var embeddingProvider = serviceProvider.GetRequiredService<EmbeddingServiceProvider>();
                await embeddingProvider.GetServiceAsync(); // Preload attempt
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to preload embedding service during startup");
            }

            // Preload one-shot service container
            try
            {
                var oneShotProvider = serviceProvider.GetRequiredService<OneShotServiceContainerProvider>();
                await oneShotProvider.GetServiceAsync(); // Preload attempt
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to preload one-shot service container during startup");
            }

            // Preload Ollama client
            try
            {
                var ollamaClientProvider = serviceProvider.GetRequiredService<OllamaClientProvider>();
                await ollamaClientProvider.GetServiceAsync(); // Preload attempt
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to preload Ollama client during startup");
            }
        }
    }
}

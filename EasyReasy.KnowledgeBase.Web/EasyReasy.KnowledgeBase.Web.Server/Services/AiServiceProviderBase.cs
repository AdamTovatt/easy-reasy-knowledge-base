using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    /// <summary>
    /// Base class for AI service providers that handles common functionality like error handling, logging, and lazy loading.
    /// </summary>
    /// <typeparam name="T">The type of AI service being provided.</typeparam>
    public abstract class AiServiceProviderBase<T> : IAiServiceProvider<T>, IServiceHealthReport where T : class
    {
        private readonly ILogger _logger;

        protected AiServiceProviderBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the current service instance, if available.
        /// </summary>
        public T? Service { get; protected set; }

        /// <summary>
        /// Gets whether the service is currently available.
        /// </summary>
        public bool IsAvailable => Service != null;

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        public string? ErrorMessage { get; protected set; }

        /// <summary>
        /// Gets the last exception that occurred, if any.
        /// </summary>
        public Exception? LastException { get; protected set; }

        /// <summary>
        /// Gets the name of the service for health reporting.
        /// </summary>
        public abstract string ServiceName { get; }

        /// <summary>
        /// Gets the service, attempting to create it if not already available.
        /// </summary>
        /// <returns>The service instance, or null if creation failed.</returns>
        public async Task<T?> GetServiceAsync()
        {
            if (Service != null)
                return Service;

            try
            {
                Service = await CreateServiceAsync();
                ErrorMessage = null;
                LastException = null;
                _logger.LogInformation("AI service {ServiceType} initialized successfully", typeof(T).Name);
                return Service;
            }
            catch (Exception ex)
            {
                LastException = ex;
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to initialize AI service {ServiceType}: {Message}", typeof(T).Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Refreshes the health status by attempting to create/reconnect to the service.
        /// </summary>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        public async Task RefreshAsync()
        {
            // Clear current service and error state
            Service = null;
            ErrorMessage = null;
            LastException = null;

            // Attempt to create the service again
            await GetServiceAsync();
        }

        /// <summary>
        /// Abstract method that derived classes must implement to create the specific service.
        /// </summary>
        /// <returns>The created service instance.</returns>
        protected abstract Task<T> CreateServiceAsync();
    }
}

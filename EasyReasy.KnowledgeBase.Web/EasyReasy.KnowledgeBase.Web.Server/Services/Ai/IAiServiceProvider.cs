namespace EasyReasy.KnowledgeBase.Web.Server.Services.Ai
{
    /// <summary>
    /// Interface for AI service providers that handle lazy loading and error handling.
    /// </summary>
    /// <typeparam name="T">The type of AI service being provided.</typeparam>
    public interface IAiServiceProvider<T> where T : class
    {
        /// <summary>
        /// Gets the current service instance, if available.
        /// </summary>
        T? Service { get; }

        /// <summary>
        /// Gets whether the service is currently available.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        string? ErrorMessage { get; }

        /// <summary>
        /// Gets the last exception that occurred, if any.
        /// </summary>
        Exception? LastException { get; }

        /// <summary>
        /// Gets the service, attempting to create it if not already available.
        /// </summary>
        /// <returns>The service instance, or null if creation failed.</returns>
        Task<T?> GetServiceAsync();
    }
}

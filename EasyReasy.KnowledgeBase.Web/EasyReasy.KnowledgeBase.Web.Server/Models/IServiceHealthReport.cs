namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Interface for services that can report their health status.
    /// </summary>
    public interface IServiceHealthReport
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Gets whether the service is currently available.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        string? ErrorMessage { get; }

        /// <summary>
        /// Refreshes the health status by attempting to create/reconnect to the service.
        /// </summary>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        Task RefreshAsync();
    }
}

namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Response model for service health status.
    /// </summary>
    public class ServiceHealthResponse
    {
        /// <summary>
        /// Gets or sets the collection of service health reports.
        /// </summary>
        public IReadOnlyCollection<IServiceHealthReport> Services { get; set; }

        /// <summary>
        /// Gets the overall health status - true if all services are available.
        /// </summary>
        public bool IsHealthy => Services.All(s => s.IsAvailable);

        /// <summary>
        /// Gets the count of available services.
        /// </summary>
        public int AvailableServicesCount => Services.Count(s => s.IsAvailable);

        /// <summary>
        /// Gets the total number of services.
        /// </summary>
        public int TotalServicesCount => Services.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHealthResponse"/> class.
        /// </summary>
        /// <param name="services">The collection of service health reports.</param>
        public ServiceHealthResponse(IReadOnlyCollection<IServiceHealthReport> services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
}

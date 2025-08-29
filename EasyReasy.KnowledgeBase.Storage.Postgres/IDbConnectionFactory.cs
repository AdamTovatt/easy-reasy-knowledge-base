using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres
{
    /// <summary>
    /// Defines the contract for creating database connections.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Creates a new database connection.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A new database connection.</returns>
        Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new database connection and opens it.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A new opened database connection.</returns>
        Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}

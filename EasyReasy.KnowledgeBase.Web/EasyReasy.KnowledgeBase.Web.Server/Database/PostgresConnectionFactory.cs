using EasyReasy.KnowledgeBase.Storage;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Database
{
    /// <summary>
    /// PostgreSQL implementation of the database connection factory for the web server.
    /// </summary>
    public class PostgresConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresConnectionFactory"/> class with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when the connection string is null.</exception>
        public PostgresConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Creates a new PostgreSQL database connection.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A new PostgreSQL database connection.</returns>
        public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDbConnection>(new NpgsqlConnection(_connectionString));
        }

        /// <summary>
        /// Creates a new PostgreSQL database connection and opens it.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A new opened PostgreSQL database connection.</returns>
        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}

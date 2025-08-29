using EasyReasy.KnowledgeBase.Storage;

namespace EasyReasy.KnowledgeBase.Storage.Postgres
{
    /// <summary>
    /// PostgreSQL-based implementation of the knowledge store that provides file, section, and chunk storage.
    /// </summary>
    public class PostgresKnowledgeStore : IKnowledgeStore
    {
        /// <summary>
        /// Gets the file store for managing knowledge files.
        /// </summary>
        public IFileStore Files { get; }

        /// <summary>
        /// Gets the chunk store for managing knowledge file chunks.
        /// </summary>
        public IChunkStore Chunks { get; }

        /// <summary>
        /// Gets the section store for managing knowledge file sections.
        /// </summary>
        public ISectionStore Sections { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresKnowledgeStore"/> class with the specified connection factory.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory to use for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when the connection factory is null.</exception>
        public PostgresKnowledgeStore(IDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
                throw new ArgumentNullException(nameof(connectionFactory));

            Files = new PostgresFileStore(connectionFactory);
            PostgresChunkStore chunkStore = new PostgresChunkStore(connectionFactory);
            Chunks = chunkStore;
            Sections = new PostgresSectionStore(connectionFactory, chunkStore);
        }

        /// <summary>
        /// Creates a new PostgreSQL knowledge store instance with the specified connection string.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        /// <returns>A new <see cref="PostgresKnowledgeStore"/> instance.</returns>
        public static PostgresKnowledgeStore Create(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            PostgresConnectionFactory connectionFactory = new PostgresConnectionFactory(connectionString);
            return new PostgresKnowledgeStore(connectionFactory);
        }

        /// <summary>
        /// Creates a new PostgreSQL knowledge store instance with the specified connection factory.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        /// <returns>A new <see cref="PostgresKnowledgeStore"/> instance.</returns>
        public static PostgresKnowledgeStore Create(IDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
                throw new ArgumentNullException(nameof(connectionFactory));

            return new PostgresKnowledgeStore(connectionFactory);
        }
    }
}

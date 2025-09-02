using EasyReasy.KnowledgeBase.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres
{
    /// <summary>
    /// PostgreSQL-based implementation of the file store for managing knowledge files.
    /// </summary>
    public class PostgresFileStore : IFileStore
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresFileStore"/> class with the specified connection factory.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory to use for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when the connection factory is null.</exception>
        public PostgresFileStore(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>
        /// Adds a new knowledge file to the store.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <returns>The unique identifier of the added file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the file is null.</exception>
        public async Task<Guid> AddAsync(KnowledgeFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "INSERT INTO knowledge_file (id, name, hash, processed_at, status) VALUES (@Id, @Name, @Hash, @ProcessedAt, @Status)",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", file.Id);
            command.Parameters.AddWithValue("@Name", file.Name);
            command.Parameters.AddWithValue("@Hash", file.Hash);
            command.Parameters.AddWithValue("@ProcessedAt", file.ProcessedAt);
            command.Parameters.AddWithValue("@Status", (int)file.Status);

            await command.ExecuteNonQueryAsync();
            return file.Id;
        }

        /// <summary>
        /// Retrieves a knowledge file by its unique identifier.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file to retrieve.</param>
        /// <returns>The knowledge file if found; otherwise, null.</returns>
        public async Task<KnowledgeFile?> GetAsync(Guid fileId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, name, hash, processed_at, status FROM knowledge_file WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", fileId);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new KnowledgeFile(
                    reader.GetGuid("id"),
                    reader.GetString("name"),
                    (byte[])reader["hash"],
                    reader.GetDateTime("processed_at"),
                    (IndexingStatus)reader.GetInt32("status"));
            }

            return null;
        }

        /// <summary>
        /// Checks if a knowledge file exists in the store.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public async Task<bool> ExistsAsync(Guid fileId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM knowledge_file WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", fileId);

            long count = (long)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        /// <summary>
        /// Retrieves all knowledge files from the store.
        /// </summary>
        /// <returns>A collection of all knowledge files.</returns>
        public async Task<IEnumerable<KnowledgeFile>> GetAllAsync()
        {

            List<KnowledgeFile> files = new List<KnowledgeFile>();

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, name, hash, processed_at, status FROM knowledge_file ORDER BY processed_at DESC",
                (NpgsqlConnection)connection);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                files.Add(new KnowledgeFile(
                    reader.GetGuid("id"),
                    reader.GetString("name"),
                    (byte[])reader["hash"],
                    reader.GetDateTime("processed_at"),
                    (IndexingStatus)reader.GetInt32("status")));
            }

            return files;
        }

        /// <summary>
        /// Updates an existing knowledge file in the store.
        /// </summary>
        /// <param name="file">The file to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file is null.</exception>
        public async Task UpdateAsync(KnowledgeFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "UPDATE knowledge_file SET name = @Name, hash = @Hash, processed_at = @ProcessedAt, status = @Status WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", file.Id);
            command.Parameters.AddWithValue("@Name", file.Name);
            command.Parameters.AddWithValue("@Hash", file.Hash);
            command.Parameters.AddWithValue("@ProcessedAt", file.ProcessedAt);
            command.Parameters.AddWithValue("@Status", (int)file.Status);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"File with ID {file.Id} not found.");
        }

        /// <summary>
        /// Deletes a knowledge file from the store.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file to delete.</param>
        /// <returns>True if the file was deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(Guid fileId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "DELETE FROM knowledge_file WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", fileId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}

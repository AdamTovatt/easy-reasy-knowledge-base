using EasyReasy.KnowledgeBase.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres
{
    /// <summary>
    /// PostgreSQL-based implementation of the chunk store for managing knowledge file chunks.
    /// </summary>
    public class PostgresChunkStore : IChunkStore
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresChunkStore"/> class with the specified connection factory.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory to use for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when the connection factory is null.</exception>
        public PostgresChunkStore(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>
        /// Converts a float array embedding to a byte array for storage.
        /// </summary>
        /// <param name="embedding">The embedding vector to convert.</param>
        /// <returns>The byte array representation of the embedding.</returns>
        private static byte[] ConvertEmbeddingToBytes(float[] embedding)
        {
            byte[] bytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Converts a byte array back to a float array embedding.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>The float array representation of the embedding.</returns>
        private static float[] ConvertBytesToEmbedding(byte[] bytes)
        {
            float[] embedding = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
            return embedding;
        }

        /// <summary>
        /// Gets the file ID from a section ID.
        /// </summary>
        /// <param name="sectionId">The section ID to look up.</param>
        /// <returns>The file ID associated with the section.</returns>
        private async Task<Guid> GetFileIdFromSectionAsync(Guid sectionId)
        {
            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT file_id FROM knowledge_section WHERE id = @SectionId",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@SectionId", sectionId);

            object? result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Section with ID {sectionId} not found.");

            return (Guid)result;
        }

        /// <summary>
        /// Adds a new knowledge file chunk to the store.
        /// </summary>
        /// <param name="chunk">The chunk to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when the chunk is null.</exception>
        public async Task AddAsync(KnowledgeFileChunk chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "INSERT INTO knowledge_chunk (id, section_id, chunk_index, content, embedding, file_id) VALUES (@Id, @SectionId, @ChunkIndex, @Content, @Embedding, @FileId)",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", chunk.Id);
            command.Parameters.AddWithValue("@SectionId", chunk.SectionId);
            command.Parameters.AddWithValue("@ChunkIndex", chunk.ChunkIndex);
            command.Parameters.AddWithValue("@Content", chunk.Content);
            command.Parameters.AddWithValue("@Embedding", chunk.Embedding != null ? ConvertEmbeddingToBytes(chunk.Embedding) : (object)DBNull.Value);
            command.Parameters.AddWithValue("@FileId", await GetFileIdFromSectionAsync(chunk.SectionId));

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves a chunk by its unique identifier.
        /// </summary>
        /// <param name="chunkId">The unique identifier of the chunk.</param>
        /// <returns>The chunk if found; otherwise, null.</returns>
        public async Task<KnowledgeFileChunk?> GetAsync(Guid chunkId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, section_id, chunk_index, content, embedding FROM knowledge_chunk WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", chunkId);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new KnowledgeFileChunk(
                    reader.GetGuid("id"),
                    reader.GetGuid("section_id"),
                    reader.GetInt32("chunk_index"),
                    reader.GetString("content"),
                    reader.IsDBNull("embedding") ? null : ConvertBytesToEmbedding((byte[])reader["embedding"]));
            }

            return null;
        }

        /// <summary>
        /// Retrieves multiple chunks by their unique identifiers.
        /// </summary>
        /// <param name="chunkIds">The collection of unique identifiers of the chunks to retrieve.</param>
        /// <returns>A list of chunks that were found.</returns>
        public async Task<IEnumerable<KnowledgeFileChunk>> GetAsync(IEnumerable<Guid> chunkIds)
        {
            if (chunkIds == null || !chunkIds.Any())
                return Enumerable.Empty<KnowledgeFileChunk>();

            List<KnowledgeFileChunk> chunks = new List<KnowledgeFileChunk>();
            Guid[] ids = chunkIds.ToArray();

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            
            // Use a parameterized query with multiple values
            string placeholders = string.Join(",", ids.Select((_, i) => $"@Id{i}"));
            using NpgsqlCommand command = new NpgsqlCommand(
                $"SELECT id, section_id, chunk_index, content, embedding FROM knowledge_chunk WHERE id IN ({placeholders})",
                (NpgsqlConnection)connection);

            for (int i = 0; i < ids.Length; i++)
            {
                command.Parameters.AddWithValue($"@Id{i}", ids[i]);
            }

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chunks.Add(new KnowledgeFileChunk(
                    reader.GetGuid("id"),
                    reader.GetGuid("section_id"),
                    reader.GetInt32("chunk_index"),
                    reader.GetString("content"),
                    reader.IsDBNull("embedding") ? null : ConvertBytesToEmbedding((byte[])reader["embedding"])));
            }

            return chunks;
        }

        /// <summary>
        /// Retrieves a chunk by its index within a specific section.
        /// </summary>
        /// <param name="sectionId">The unique identifier of the section.</param>
        /// <param name="chunkIndex">The zero-based index of the chunk within the section.</param>
        /// <returns>The chunk if found; otherwise, null.</returns>
        public async Task<KnowledgeFileChunk?> GetByIndexAsync(Guid sectionId, int chunkIndex)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, section_id, chunk_index, content, embedding FROM knowledge_chunk WHERE section_id = @SectionId AND chunk_index = @ChunkIndex",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@SectionId", sectionId);
            command.Parameters.AddWithValue("@ChunkIndex", chunkIndex);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new KnowledgeFileChunk(
                    reader.GetGuid("id"),
                    reader.GetGuid("section_id"),
                    reader.GetInt32("chunk_index"),
                    reader.GetString("content"),
                    reader.IsDBNull("embedding") ? null : ConvertBytesToEmbedding((byte[])reader["embedding"]));
            }

            return null;
        }

        /// <summary>
        /// Retrieves all chunks belonging to a specific section.
        /// </summary>
        /// <param name="sectionId">The unique identifier of the section.</param>
        /// <returns>A collection of chunks belonging to the section, ordered by their index.</returns>
        public async Task<IEnumerable<KnowledgeFileChunk>> GetBySectionAsync(Guid sectionId)
        {

            List<KnowledgeFileChunk> chunks = new List<KnowledgeFileChunk>();

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, section_id, chunk_index, content, embedding FROM knowledge_chunk WHERE section_id = @SectionId ORDER BY chunk_index",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@SectionId", sectionId);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                chunks.Add(new KnowledgeFileChunk(
                    reader.GetGuid("id"),
                    reader.GetGuid("section_id"),
                    reader.GetInt32("chunk_index"),
                    reader.GetString("content"),
                    reader.IsDBNull("embedding") ? null : ConvertBytesToEmbedding((byte[])reader["embedding"])));
            }

            return chunks;
        }

        /// <summary>
        /// Deletes all chunks belonging to a specific file.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file whose chunks should be deleted.</param>
        /// <returns>True if any chunks were deleted; otherwise, false.</returns>
        public async Task<bool> DeleteByFileAsync(Guid fileId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "DELETE FROM knowledge_chunk WHERE file_id = @FileId",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@FileId", fileId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}

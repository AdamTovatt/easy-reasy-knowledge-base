using EasyReasy.KnowledgeBase.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres
{
    /// <summary>
    /// PostgreSQL-based implementation of the section store for managing knowledge file sections.
    /// </summary>
    public class PostgresSectionStore : ISectionStore
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IChunkStore _chunkStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresSectionStore"/> class with the specified connection factory and chunk store.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory to use for database operations.</param>
        /// <param name="chunkStore">The chunk store to use for loading chunks.</param>
        /// <exception cref="ArgumentNullException">Thrown when the connection factory or chunk store is null.</exception>
        public PostgresSectionStore(IDbConnectionFactory connectionFactory, IChunkStore chunkStore)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _chunkStore = chunkStore ?? throw new ArgumentNullException(nameof(chunkStore));
        }

        /// <summary>
        /// Adds a new knowledge file section to the store.
        /// </summary>
        /// <param name="section">The section to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when the section is null.</exception>
        public async Task AddAsync(KnowledgeFileSection section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "INSERT INTO knowledge_section (id, file_id, section_index, summary, additional_context) VALUES (@Id, @FileId, @SectionIndex, @Summary, @AdditionalContext)",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", section.Id);
            command.Parameters.AddWithValue("@FileId", section.FileId);
            command.Parameters.AddWithValue("@SectionIndex", section.SectionIndex);
            command.Parameters.AddWithValue("@Summary", section.Summary ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AdditionalContext", section.AdditionalContext ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Retrieves a section by its unique identifier.
        /// </summary>
        /// <param name="sectionId">The unique identifier of the section.</param>
        /// <returns>The section if found; otherwise, null.</returns>
        public async Task<KnowledgeFileSection?> GetAsync(Guid sectionId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, file_id, section_index, summary, additional_context FROM knowledge_section WHERE id = @Id",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@Id", sectionId);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Guid id = reader.GetGuid("id");
                Guid fileId = reader.GetGuid("file_id");
                int sectionIndex = reader.GetInt32("section_index");
                string? summary = reader.IsDBNull("summary") ? null : reader.GetString("summary");
                string? additionalContext = reader.IsDBNull("additional_context") ? null : reader.GetString("additional_context");

                // Load chunks for this section
                IEnumerable<KnowledgeFileChunk> chunks = await _chunkStore.GetBySectionAsync(id);

                return new KnowledgeFileSection(id, fileId, sectionIndex, chunks.ToList(), summary)
                {
                    AdditionalContext = additionalContext
                };
            }

            return null;
        }

        /// <summary>
        /// Retrieves a section by its index within a specific file.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file.</param>
        /// <param name="sectionIndex">The zero-based index of the section within the file.</param>
        /// <returns>The section if found; otherwise, null.</returns>
        public async Task<KnowledgeFileSection?> GetByIndexAsync(Guid fileId, int sectionIndex)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id, file_id, section_index, summary, additional_context FROM knowledge_section WHERE file_id = @FileId AND section_index = @SectionIndex",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@FileId", fileId);
            command.Parameters.AddWithValue("@SectionIndex", sectionIndex);

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Guid id = reader.GetGuid("id");
                string? summary = reader.IsDBNull("summary") ? null : reader.GetString("summary");
                string? additionalContext = reader.IsDBNull("additional_context") ? null : reader.GetString("additional_context");

                // Load chunks for this section
                IEnumerable<KnowledgeFileChunk> chunks = await _chunkStore.GetBySectionAsync(id);

                return new KnowledgeFileSection(id, fileId, sectionIndex, chunks.ToList(), summary);
            }

            return null;
        }

        /// <summary>
        /// Deletes all sections belonging to a specific file.
        /// </summary>
        /// <param name="fileId">The unique identifier of the file whose sections should be deleted.</param>
        /// <returns>True if any sections were deleted; otherwise, false.</returns>
        public async Task<bool> DeleteByFileAsync(Guid fileId)
        {

            using IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync();
            using NpgsqlCommand command = new NpgsqlCommand(
                "DELETE FROM knowledge_section WHERE file_id = @FileId",
                (NpgsqlConnection)connection);

            command.Parameters.AddWithValue("@FileId", fileId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}

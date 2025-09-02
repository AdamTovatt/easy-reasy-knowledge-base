using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Storage;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements file data access operations using PostgreSQL.
    /// </summary>
    public class KnowledgeFileRepository : IKnowledgeFileRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeFileRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public KnowledgeFileRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<KnowledgeFile> CreateAsync(
            Guid knowledgeBaseId,
            string originalKnowledgeFileName,
            string contentType,
            long sizeInBytes,
            string relativePath,
            Guid uploadedByUserId)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(originalKnowledgeFileName))
                throw new ArgumentException("Original file name cannot be null, empty, or whitespace.", nameof(originalKnowledgeFileName));

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be null, empty, or whitespace.", nameof(contentType));

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path cannot be null, empty, or whitespace.", nameof(relativePath));

            if (sizeInBytes < 0)
                throw new ArgumentException("KnowledgeFile size cannot be negative.", nameof(sizeInBytes));

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string insertKnowledgeFileSql = @"
                        INSERT INTO knowledge_file (knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at)
                        VALUES (@knowledgeBaseId, @originalKnowledgeFileName, @contentType, @sizeInBytes, @relativePath, @uploadedByUserId, @uploadedAt)
                        RETURNING id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at";

                    DateTime uploadedAt = DateTime.UtcNow;
                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(insertKnowledgeFileSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                        command.Parameters.AddWithValue("@originalKnowledgeFileName", originalKnowledgeFileName);
                        command.Parameters.AddWithValue("@contentType", contentType);
                        command.Parameters.AddWithValue("@sizeInBytes", sizeInBytes);
                        command.Parameters.AddWithValue("@relativePath", relativePath);
                        command.Parameters.AddWithValue("@uploadedByUserId", uploadedByUserId);
                        command.Parameters.AddWithValue("@uploadedAt", uploadedAt);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException("Failed to create file record");
                            }

                            return new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    // Handle specific PostgreSQL constraint violations
                    switch (ex.SqlState)
                    {
                        case "23505": // unique_violation
                            throw new InvalidOperationException("A file with this information already exists.", ex);
                        case "23502": // not_null_violation
                            throw new InvalidOperationException("A required field is missing.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced knowledge base or user does not exist.", ex);
                        case "22001": // string_data_right_truncation
                            throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeFile?> GetByIdAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeFileByParameterAsync("id", id, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeFile?> GetByIdInKnowledgeBaseAsync(Guid knowledgeBaseId, Guid fileId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFileSql = @"
                    SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM knowledge_file
                    WHERE id = @fileId AND knowledge_base_id = @knowledgeBaseId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFileSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@fileId", fileId);
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            );
                        }
                        return null;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeFile> UpdateAsync(KnowledgeFile file)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string updateKnowledgeFileSql = @"
                        UPDATE knowledge_file 
                        SET knowledge_base_id = @knowledgeBaseId, original_file_name = @originalKnowledgeFileName, 
                            content_type = @contentType, size_in_bytes = @sizeInBytes, relative_path = @relativePath, 
                            uploaded_by_user_id = @uploadedByUserId, uploaded_at = @uploadedAt
                        WHERE id = @id
                        RETURNING id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(updateKnowledgeFileSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@id", file.Id);
                        command.Parameters.AddWithValue("@knowledgeBaseId", file.KnowledgeBaseId);
                        command.Parameters.AddWithValue("@originalKnowledgeFileName", file.OriginalFileName);
                        command.Parameters.AddWithValue("@contentType", file.ContentType);
                        command.Parameters.AddWithValue("@sizeInBytes", file.SizeInBytes);
                        command.Parameters.AddWithValue("@relativePath", file.RelativePath);
                        command.Parameters.AddWithValue("@uploadedByUserId", file.UploadedByUserId);
                        command.Parameters.AddWithValue("@uploadedAt", file.UploadedAt);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException($"KnowledgeFile with ID {file.Id} not found");
                            }

                            return new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    // Handle specific PostgreSQL constraint violations
                    switch (ex.SqlState)
                    {
                        case "23505": // unique_violation
                            throw new InvalidOperationException("A file with this information already exists.", ex);
                        case "23502": // not_null_violation
                            throw new InvalidOperationException("A required field is missing.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced knowledge base or user does not exist.", ex);
                        case "22001": // string_data_right_truncation
                            throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string deleteKnowledgeFileSql = @"
                        DELETE FROM knowledge_file 
                        WHERE id = @id";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(deleteKnowledgeFileSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (PostgresException ex)
                {
                    // Handle specific PostgreSQL constraint violations
                    switch (ex.SqlState)
                    {
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Cannot delete file due to existing references.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM knowledge_file
                    WHERE knowledge_base_id = @knowledgeBaseId
                    ORDER BY uploaded_at DESC";

                List<KnowledgeFile> files = new List<KnowledgeFile>();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            ));
                        }
                    }
                }

                return files;
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId, int offset, int limit)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM knowledge_file
                    WHERE knowledge_base_id = @knowledgeBaseId
                    ORDER BY uploaded_at DESC
                    OFFSET @offset LIMIT @limit";

                List<KnowledgeFile> files = new List<KnowledgeFile>();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            ));
                        }
                    }
                }

                return files;
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeFile>> GetByUserIdAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeFilesByParameterAsync("uploaded_by_user_id", userId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAndUserIdAsync(Guid knowledgeBaseId, Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM knowledge_file
                    WHERE knowledge_base_id = @knowledgeBaseId AND uploaded_by_user_id = @userId
                    ORDER BY uploaded_at DESC";

                List<KnowledgeFile> files = new List<KnowledgeFile>();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new KnowledgeFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                                uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            ));
                        }
                    }
                }

                return files;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string existsSql = @"
                    SELECT COUNT(1)
                    FROM knowledge_file
                    WHERE id = @id";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(existsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    object? result = await command.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsInKnowledgeBaseAsync(Guid knowledgeBaseId, Guid fileId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string existsSql = @"
                    SELECT COUNT(1)
                    FROM knowledge_file
                    WHERE id = @fileId AND knowledge_base_id = @knowledgeBaseId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(existsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@fileId", fileId);
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    object? result = await command.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string countSql = @"
                    SELECT COUNT(*)
                    FROM knowledge_file
                    WHERE knowledge_base_id = @knowledgeBaseId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(countSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    object? result = await command.ExecuteScalarAsync();
                    return result == null ? 0 : (long)result;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetTotalSizeByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string sizeSql = @"
                    SELECT COALESCE(SUM(size_in_bytes), 0)
                    FROM knowledge_file
                    WHERE knowledge_base_id = @knowledgeBaseId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(sizeSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    object? result = await command.ExecuteScalarAsync();
                    return result == null ? 0 : Convert.ToInt64(result);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByKnowledgeBaseIdAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string deleteKnowledgeFilesSql = @"
                        DELETE FROM knowledge_file 
                        WHERE knowledge_base_id = @knowledgeBaseId";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(deleteKnowledgeFilesSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
                catch (PostgresException ex)
                {
                    // Handle specific PostgreSQL constraint violations
                    switch (ex.SqlState)
                    {
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Cannot delete files due to existing references.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a file by a specific parameter using an existing connection.
        /// </summary>
        /// <typeparam name="T">The type of the parameter value.</typeparam>
        /// <param name="parameterName">The name of the parameter (e.g., "id", "knowledge_base_id").</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <param name="connection">The open database connection to use.</param>
        /// <returns>The file if found, null otherwise.</returns>
        private async Task<KnowledgeFile?> GetKnowledgeFileByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getKnowledgeFileSql = $@"
                SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                FROM knowledge_file
                WHERE {parameterName} = @{parameterName}";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFileSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new KnowledgeFile(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                            originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                            contentType: reader.GetString(reader.GetOrdinal("content_type")),
                            sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                            relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                            uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                            uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                            updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                        );
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieves files by a specific parameter using an existing connection.
        /// </summary>
        /// <typeparam name="T">The type of the parameter value.</typeparam>
        /// <param name="parameterName">The name of the parameter (e.g., "knowledge_base_id", "uploaded_by_user_id").</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <param name="connection">The open database connection to use.</param>
        /// <returns>A list of files matching the parameter.</returns>
        private async Task<List<KnowledgeFile>> GetKnowledgeFilesByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getKnowledgeFilesSql = $@"
                SELECT id, knowledge_base_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at
                FROM knowledge_file
                WHERE {parameterName} = @{parameterName}
                ORDER BY uploaded_at DESC";

            List<KnowledgeFile> files = new List<KnowledgeFile>();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(getKnowledgeFilesSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        files.Add(new KnowledgeFile(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                            originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                            contentType: reader.GetString(reader.GetOrdinal("content_type")),
                            sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                            relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                            uploadedByUserId: reader.GetGuid(reader.GetOrdinal("uploaded_by_user_id")),
                            uploadedAt: reader.GetDateTime(reader.GetOrdinal("uploaded_at")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                            updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                        ));
                    }
                }
            }

            return files;
        }
    }
}

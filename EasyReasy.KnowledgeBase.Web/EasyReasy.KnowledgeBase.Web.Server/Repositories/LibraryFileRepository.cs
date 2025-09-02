using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements file data access operations using PostgreSQL.
    /// </summary>
    public class LibraryFileRepository : ILibraryFileRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryFileRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public LibraryFileRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<LibraryFile> CreateAsync(
            Guid libraryId,
            string originalKnowledgeFileName,
            string contentType,
            long sizeInBytes,
            string relativePath,
            byte[] hash,
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

            if (hash == null || hash.Length == 0)
                throw new ArgumentException("File hash cannot be null or empty.", nameof(hash));

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string insertKnowledgeFileSql = @"
                        INSERT INTO library_file (library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at)
                        VALUES (@libraryId, @originalKnowledgeFileName, @contentType, @sizeInBytes, @relativePath, @fileHash, @uploadedByUserId, @uploadedAt)
                        RETURNING id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at";

                    DateTime uploadedAt = DateTime.UtcNow;
                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(insertKnowledgeFileSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@libraryId", libraryId);
                        command.Parameters.AddWithValue("@originalKnowledgeFileName", originalKnowledgeFileName);
                        command.Parameters.AddWithValue("@contentType", contentType);
                        command.Parameters.AddWithValue("@sizeInBytes", sizeInBytes);
                        command.Parameters.AddWithValue("@relativePath", relativePath);
                        command.Parameters.AddWithValue("@fileHash", hash);
                        command.Parameters.AddWithValue("@uploadedByUserId", uploadedByUserId);
                        command.Parameters.AddWithValue("@uploadedAt", uploadedAt);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException("Failed to create file record");
                            }

                            return new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
                            throw new InvalidOperationException("Referenced library or user does not exist.", ex);
                        case "22001": // string_data_right_truncation
                            throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryFile?> GetByIdAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeFileByParameterAsync("id", id, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryFile?> GetByIdInKnowledgeBaseAsync(Guid libraryId, Guid fileId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFileSql = @"
                    SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM library_file
                    WHERE id = @fileId AND library_id = @libraryId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(getKnowledgeFileSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@fileId", fileId);
                    command.Parameters.AddWithValue("@libraryId", libraryId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
        public async Task<LibraryFile> UpdateAsync(LibraryFile file)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string updateKnowledgeFileSql = @"
                        UPDATE library_file 
                        SET library_id = @libraryId, original_file_name = @originalKnowledgeFileName, 
                            content_type = @contentType, size_in_bytes = @sizeInBytes, relative_path = @relativePath, 
                            uploaded_by_user_id = @uploadedByUserId, uploaded_at = @uploadedAt
                        WHERE id = @id
                        RETURNING id, library_id, original_file_name, content_type, size_in_bytes, relative_path, uploaded_by_user_id, uploaded_at, created_at, updated_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(updateKnowledgeFileSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@id", file.Id);
                        command.Parameters.AddWithValue("@libraryId", file.LibraryId);
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
                                throw new InvalidOperationException($"LibraryFile with ID {file.Id} not found");
                            }

                            return new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
                            throw new InvalidOperationException("Referenced library or user does not exist.", ex);
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
                        DELETE FROM library_file 
                        WHERE id = @id";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(deleteKnowledgeFileSql, npgsqlConnection))
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
        public async Task<List<LibraryFile>> GetByKnowledgeBaseIdAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM library_file
                    WHERE library_id = @libraryId
                    ORDER BY uploaded_at DESC";

                List<LibraryFile> files = new();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
        public async Task<List<LibraryFile>> GetByKnowledgeBaseIdAsync(Guid libraryId, int offset, int limit)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM library_file
                    WHERE library_id = @libraryId
                    ORDER BY uploaded_at DESC
                    OFFSET @offset LIMIT @limit";

                List<LibraryFile> files = new();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
        public async Task<List<LibraryFile>> GetByUserIdAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeFilesByParameterAsync("uploaded_by_user_id", userId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<LibraryFile>> GetByKnowledgeBaseIdAndUserIdAsync(Guid libraryId, Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string getKnowledgeFilesSql = @"
                    SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                    FROM library_file
                    WHERE library_id = @libraryId AND uploaded_by_user_id = @userId
                    ORDER BY uploaded_at DESC";

                List<LibraryFile> files = new();
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(getKnowledgeFilesSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    command.Parameters.AddWithValue("@userId", userId);

                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            files.Add(new LibraryFile(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                                originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                                contentType: reader.GetString(reader.GetOrdinal("content_type")),
                                sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                                relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                                hash: (byte[])reader["file_hash"],
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
                    FROM library_file
                    WHERE id = @id";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(existsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    object? result = await command.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsInKnowledgeBaseAsync(Guid libraryId, Guid fileId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string existsSql = @"
                    SELECT COUNT(1)
                    FROM library_file
                    WHERE id = @fileId AND library_id = @libraryId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(existsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@fileId", fileId);
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    object? result = await command.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetCountByKnowledgeBaseIdAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string countSql = @"
                    SELECT COUNT(*)
                    FROM library_file
                    WHERE library_id = @libraryId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(countSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    object? result = await command.ExecuteScalarAsync();
                    return result == null ? 0 : (long)result;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetTotalSizeByKnowledgeBaseIdAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string sizeSql = @"
                    SELECT COALESCE(SUM(size_in_bytes), 0)
                    FROM library_file
                    WHERE library_id = @libraryId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(sizeSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    object? result = await command.ExecuteScalarAsync();
                    return result == null ? 0 : Convert.ToInt64(result);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByKnowledgeBaseIdAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string deleteKnowledgeFilesSql = @"
                        DELETE FROM library_file 
                        WHERE library_id = @libraryId";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(deleteKnowledgeFilesSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@libraryId", libraryId);
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
        private async Task<LibraryFile?> GetKnowledgeFileByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getKnowledgeFileSql = $@"
                SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                FROM library_file
                WHERE {parameterName} = @{parameterName}";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getKnowledgeFileSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new LibraryFile(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                            originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                            contentType: reader.GetString(reader.GetOrdinal("content_type")),
                            sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                            relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                            hash: (byte[])reader["file_hash"],
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
        private async Task<List<LibraryFile>> GetKnowledgeFilesByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getKnowledgeFilesSql = $@"
                SELECT id, library_id, original_file_name, content_type, size_in_bytes, relative_path, file_hash, uploaded_by_user_id, uploaded_at, created_at, updated_at
                FROM library_file
                WHERE {parameterName} = @{parameterName}
                ORDER BY uploaded_at DESC";

            List<LibraryFile> files = new();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getKnowledgeFilesSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        files.Add(new LibraryFile(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            libraryId: reader.GetGuid(reader.GetOrdinal("library_id")),
                            originalFileName: reader.GetString(reader.GetOrdinal("original_file_name")),
                            contentType: reader.GetString(reader.GetOrdinal("content_type")),
                            sizeInBytes: reader.GetInt64(reader.GetOrdinal("size_in_bytes")),
                            relativePath: reader.GetString(reader.GetOrdinal("relative_path")),
                            hash: (byte[])reader["file_hash"],
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

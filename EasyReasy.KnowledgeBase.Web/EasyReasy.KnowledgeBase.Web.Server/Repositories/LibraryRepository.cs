using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements library data access operations using PostgreSQL.
    /// </summary>
    public class LibraryRepository : ILibraryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public LibraryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<Library> CreateAsync(string name, string? description, Guid ownerId, bool isPublic = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Knowledge base name cannot be null, empty, or whitespace.", nameof(name));

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string insertKnowledgeBaseSql = @"
                        INSERT INTO knowledge_base (name, description, owner_id, is_public)
                        VALUES (@name, @description, @ownerId, @isPublic)
                        RETURNING id, name, description, owner_id, is_public, created_at, updated_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(insertKnowledgeBaseSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ownerId", ownerId);
                        command.Parameters.AddWithValue("@isPublic", isPublic);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException("Failed to create library record");
                            }

                            return new Library(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                name: reader.GetString(reader.GetOrdinal("name")),
                                description: reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                ownerId: reader.GetGuid(reader.GetOrdinal("owner_id")),
                                isPublic: reader.GetBoolean(reader.GetOrdinal("is_public")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    switch (ex.SqlState)
                    {
                        case "23505": // unique_violation
                            throw new InvalidOperationException($"A library with the name '{name}' already exists.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced user does not exist.", ex);
                        case "22001": // string_data_right_truncation
                            throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<Library?> GetByIdAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeBaseByParameterAsync("id", id, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<Library?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetKnowledgeBaseByParameterAsync("name", name, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<Library> UpdateAsync(Library knowledgeBase)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string updateKnowledgeBaseSql = @"
                        UPDATE knowledge_base 
                        SET name = @name, description = @description, owner_id = @ownerId, is_public = @isPublic
                        WHERE id = @id
                        RETURNING id, name, description, owner_id, is_public, created_at, updated_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(updateKnowledgeBaseSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@id", knowledgeBase.Id);
                        command.Parameters.AddWithValue("@name", knowledgeBase.Name);
                        command.Parameters.AddWithValue("@description", (object?)knowledgeBase.Description ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ownerId", knowledgeBase.OwnerId);
                        command.Parameters.AddWithValue("@isPublic", knowledgeBase.IsPublic);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException($"Knowledge base with ID {knowledgeBase.Id} not found");
                            }

                            return new Library(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                name: reader.GetString(reader.GetOrdinal("name")),
                                description: reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                ownerId: reader.GetGuid(reader.GetOrdinal("owner_id")),
                                isPublic: reader.GetBoolean(reader.GetOrdinal("is_public")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                                updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    switch (ex.SqlState)
                    {
                        case "23505": // unique_violation
                            throw new InvalidOperationException($"A library with the name '{knowledgeBase.Name}' already exists.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced user does not exist.", ex);
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
                    string deleteKnowledgeBaseSql = @"
                        DELETE FROM knowledge_base 
                        WHERE id = @id";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(deleteKnowledgeBaseSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
                catch (PostgresException ex)
                {
                    switch (ex.SqlState)
                    {
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Cannot delete library due to existing references.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<Library>> GetByOwnerIdAsync(Guid ownerId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetLibrariesByParameterAsync("owner_id", ownerId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<Library>> GetPublicLibrariesAsync()
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetLibrariesByParameterAsync("is_public", true, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string existsSql = @"
                    SELECT COUNT(1)
                    FROM knowledge_base
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
        public async Task<bool> IsOwnerAsync(Guid libraryId, Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string isOwnerSql = @"
                    SELECT COUNT(1)
                    FROM knowledge_base
                    WHERE id = @libraryId AND owner_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(isOwnerSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    command.Parameters.AddWithValue("@userId", userId);
                    object? result = await command.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        private async Task<Library?> GetKnowledgeBaseByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getKnowledgeBaseSql = $@"
                SELECT id, name, description, owner_id, is_public, created_at, updated_at
                FROM knowledge_base
                WHERE {parameterName} = @{parameterName}";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getKnowledgeBaseSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Library(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            name: reader.GetString(reader.GetOrdinal("name")),
                            description: reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                            ownerId: reader.GetGuid(reader.GetOrdinal("owner_id")),
                            isPublic: reader.GetBoolean(reader.GetOrdinal("is_public")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                            updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                        );
                    }
                    return null;
                }
            }
        }

        private async Task<List<Library>> GetLibrariesByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getLibrariesSql = $@"
                SELECT id, name, description, owner_id, is_public, created_at, updated_at
                FROM knowledge_base
                WHERE {parameterName} = @{parameterName}
                ORDER BY created_at DESC";

            List<Library> libraries = new();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getLibrariesSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        libraries.Add(new Library(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            name: reader.GetString(reader.GetOrdinal("name")),
                            description: reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                            ownerId: reader.GetGuid(reader.GetOrdinal("owner_id")),
                            isPublic: reader.GetBoolean(reader.GetOrdinal("is_public")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
                            updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
                        ));
                    }
                }
            }

            return libraries;
        }
    }
}

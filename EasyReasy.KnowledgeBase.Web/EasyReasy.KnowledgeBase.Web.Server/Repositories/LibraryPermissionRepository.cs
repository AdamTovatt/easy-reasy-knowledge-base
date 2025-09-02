using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements library permission data access operations using PostgreSQL.
    /// </summary>
    public class LibraryPermissionRepository : ILibraryPermissionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryPermissionRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public LibraryPermissionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<bool> HasPermissionAsync(Guid userId, Guid libraryId, LibraryPermissionType permissionType)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                // Check if user is owner (owners always have admin permissions)
                if (await IsOwnerAsync(userId, libraryId, connection))
                    return true;

                // Check if library is public and permission is read
                if (permissionType == LibraryPermissionType.Read && await IsPublicAsync(libraryId, connection))
                    return true;

                // Check explicit permission
                LibraryPermissionType? userPermission = await GetUserPermissionAsync(userId, libraryId, connection);
                if (userPermission == null)
                    return false;

                // Check if user's permission level is sufficient
                return HasSufficientPermission(userPermission.Value, permissionType);
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryPermissionType?> GetUserPermissionAsync(Guid userId, Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetUserPermissionAsync(userId, libraryId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, LibraryPermissionType minimumPermission = LibraryPermissionType.Read)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                List<Guid> accessibleIds = new();

                // Get owned librarys (owners always have admin access)
                string getOwnedSql = @"
                    SELECT id
                    FROM knowledge_base
                    WHERE owner_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(getOwnedSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            accessibleIds.Add(reader.GetGuid(reader.GetOrdinal("id")));
                        }
                    }
                }

                // Get public librarys if only read permission is required
                if (minimumPermission == LibraryPermissionType.Read)
                {
                    string getPublicSql = @"
                        SELECT id
                        FROM knowledge_base
                        WHERE is_public = true AND owner_id != @userId";

                    using (NpgsqlCommand command = new(getPublicSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accessibleIds.Add(reader.GetGuid(reader.GetOrdinal("id")));
                            }
                        }
                    }
                }

                // Get librarys with explicit permissions
                string getExplicitPermissionsSql = @"
                    SELECT knowledge_base_id
                    FROM knowledge_base_permission
                    WHERE user_id = @userId AND permission_type::text = ANY(@permissionTypes)";

                List<string> allowedPermissions = GetSufficientPermissionTypes(minimumPermission);
                using (NpgsqlCommand command = new(getExplicitPermissionsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@permissionTypes", allowedPermissions.ToArray());
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Guid kbId = reader.GetGuid(reader.GetOrdinal("knowledge_base_id"));
                            if (!accessibleIds.Contains(kbId))
                            {
                                accessibleIds.Add(kbId);
                            }
                        }
                    }
                }

                return accessibleIds;
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryPermission> GrantPermissionAsync(Guid libraryId, Guid userId, LibraryPermissionType permissionType, Guid grantedByUserId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string insertPermissionSql = @"
                        INSERT INTO knowledge_base_permission (knowledge_base_id, user_id, permission_type, granted_by_user_id)
                        VALUES (@libraryId, @userId, @permissionType, @grantedByUserId)
                        RETURNING id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(insertPermissionSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@libraryId", libraryId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@permissionType", permissionType.ToString().ToLowerInvariant());
                        command.Parameters.AddWithValue("@grantedByUserId", grantedByUserId);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException("Failed to create permission record");
                            }

                            return new LibraryPermission(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                userId: reader.GetGuid(reader.GetOrdinal("user_id")),
                                permissionType: ParsePermissionType(reader.GetString(reader.GetOrdinal("permission_type"))),
                                grantedByUserId: reader.GetGuid(reader.GetOrdinal("granted_by_user_id")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    switch (ex.SqlState)
                    {
                        case "23505": // unique_violation
                            throw new InvalidOperationException("Permission already exists for this user and library.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced library or user does not exist.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryPermission?> UpdatePermissionAsync(Guid libraryId, Guid userId, LibraryPermissionType newPermissionType, Guid updatedByUserId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string updatePermissionSql = @"
                        UPDATE knowledge_base_permission 
                        SET permission_type = @newPermissionType, granted_by_user_id = @updatedByUserId
                        WHERE knowledge_base_id = @libraryId AND user_id = @userId
                        RETURNING id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new(updatePermissionSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@libraryId", libraryId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@newPermissionType", newPermissionType.ToString().ToLowerInvariant());
                        command.Parameters.AddWithValue("@updatedByUserId", updatedByUserId);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return null;
                            }

                            return new LibraryPermission(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                libraryId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                                userId: reader.GetGuid(reader.GetOrdinal("user_id")),
                                permissionType: ParsePermissionType(reader.GetString(reader.GetOrdinal("permission_type"))),
                                grantedByUserId: reader.GetGuid(reader.GetOrdinal("granted_by_user_id")),
                                createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                            );
                        }
                    }
                }
                catch (PostgresException ex)
                {
                    switch (ex.SqlState)
                    {
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced library or user does not exist.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RevokePermissionAsync(Guid libraryId, Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string deletePermissionSql = @"
                    DELETE FROM knowledge_base_permission 
                    WHERE knowledge_base_id = @libraryId AND user_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(deletePermissionSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<LibraryPermission>> GetPermissionsByKnowledgeBaseAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetPermissionsByParameterAsync("knowledge_base_id", libraryId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<LibraryPermission>> GetPermissionsByUserAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetPermissionsByParameterAsync("user_id", userId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<int> RevokeAllPermissionsForKnowledgeBaseAsync(Guid libraryId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string deletePermissionsSql = @"
                    DELETE FROM knowledge_base_permission 
                    WHERE knowledge_base_id = @libraryId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(deletePermissionsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@libraryId", libraryId);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> RevokeAllPermissionsForUserAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string deletePermissionsSql = @"
                    DELETE FROM knowledge_base_permission 
                    WHERE user_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new(deletePermissionsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<LibraryPermissionType?> GetUserPermissionAsync(Guid userId, Guid libraryId, IDbConnection connection)
        {
            string getPermissionSql = @"
                SELECT permission_type
                FROM knowledge_base_permission
                WHERE user_id = @userId AND knowledge_base_id = @libraryId";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getPermissionSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@libraryId", libraryId);
                object? result = await command.ExecuteScalarAsync();
                if (result == null)
                    return null;

                return ParsePermissionType((string)result);
            }
        }

        private async Task<bool> IsOwnerAsync(Guid userId, Guid libraryId, IDbConnection connection)
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

        private async Task<bool> IsPublicAsync(Guid libraryId, IDbConnection connection)
        {
            string isPublicSql = @"
                SELECT is_public
                FROM knowledge_base
                WHERE id = @libraryId";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(isPublicSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue("@libraryId", libraryId);
                object? result = await command.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
        }

        private async Task<List<LibraryPermission>> GetPermissionsByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getPermissionsSql = $@"
                SELECT id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at
                FROM knowledge_base_permission
                WHERE {parameterName} = @{parameterName}
                ORDER BY created_at DESC";

            List<LibraryPermission> permissions = new();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new(getPermissionsSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(new LibraryPermission(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            libraryId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
                            userId: reader.GetGuid(reader.GetOrdinal("user_id")),
                            permissionType: ParsePermissionType(reader.GetString(reader.GetOrdinal("permission_type"))),
                            grantedByUserId: reader.GetGuid(reader.GetOrdinal("granted_by_user_id")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                        ));
                    }
                }
            }

            return permissions;
        }

        private static LibraryPermissionType ParsePermissionType(string permissionTypeString)
        {
            return permissionTypeString.ToLowerInvariant() switch
            {
                "read" => LibraryPermissionType.Read,
                "write" => LibraryPermissionType.Write,
                "admin" => LibraryPermissionType.Admin,
                _ => throw new ArgumentException($"Unknown permission type: {permissionTypeString}")
            };
        }

        private static bool HasSufficientPermission(LibraryPermissionType userPermission, LibraryPermissionType requiredPermission)
        {
            // Permission hierarchy: Read < Write < Admin
            return userPermission >= requiredPermission;
        }

        private static List<string> GetSufficientPermissionTypes(LibraryPermissionType minimumPermission)
        {
            return minimumPermission switch
            {
                LibraryPermissionType.Read => new List<string> { "read", "write", "admin" },
                LibraryPermissionType.Write => new List<string> { "write", "admin" },
                LibraryPermissionType.Admin => new List<string> { "admin" },
                _ => throw new ArgumentException($"Unknown permission type: {minimumPermission}")
            };
        }
    }
}

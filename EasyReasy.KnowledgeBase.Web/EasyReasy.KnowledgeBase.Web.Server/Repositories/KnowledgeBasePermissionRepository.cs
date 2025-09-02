using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Storage;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements knowledge base permission data access operations using PostgreSQL.
    /// </summary>
    public class KnowledgeBasePermissionRepository : IKnowledgeBasePermissionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeBasePermissionRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public KnowledgeBasePermissionRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <inheritdoc/>
        public async Task<bool> HasPermissionAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType permissionType)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                // Check if user is owner (owners always have admin permissions)
                if (await IsOwnerAsync(userId, knowledgeBaseId, connection))
                    return true;

                // Check if knowledge base is public and permission is read
                if (permissionType == KnowledgeBasePermissionType.Read && await IsPublicAsync(knowledgeBaseId, connection))
                    return true;

                // Check explicit permission
                KnowledgeBasePermissionType? userPermission = await GetUserPermissionAsync(userId, knowledgeBaseId, connection);
                if (userPermission == null)
                    return false;

                // Check if user's permission level is sufficient
                return HasSufficientPermission(userPermission.Value, permissionType);
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeBasePermissionType?> GetUserPermissionAsync(Guid userId, Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetUserPermissionAsync(userId, knowledgeBaseId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, KnowledgeBasePermissionType minimumPermission = KnowledgeBasePermissionType.Read)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                List<Guid> accessibleIds = new List<Guid>();

                // Get owned knowledge bases (owners always have admin access)
                string getOwnedSql = @"
                    SELECT id
                    FROM knowledge_base
                    WHERE owner_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(getOwnedSql, npgsqlConnection))
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

                // Get public knowledge bases if only read permission is required
                if (minimumPermission == KnowledgeBasePermissionType.Read)
                {
                    string getPublicSql = @"
                        SELECT id
                        FROM knowledge_base
                        WHERE is_public = true AND owner_id != @userId";

                    using (NpgsqlCommand command = new NpgsqlCommand(getPublicSql, npgsqlConnection))
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

                // Get knowledge bases with explicit permissions
                string getExplicitPermissionsSql = @"
                    SELECT knowledge_base_id
                    FROM knowledge_base_permission
                    WHERE user_id = @userId AND permission_type::text = ANY(@permissionTypes)";

                List<string> allowedPermissions = GetSufficientPermissionTypes(minimumPermission);
                using (NpgsqlCommand command = new NpgsqlCommand(getExplicitPermissionsSql, npgsqlConnection))
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
        public async Task<KnowledgeBasePermission> GrantPermissionAsync(Guid knowledgeBaseId, Guid userId, KnowledgeBasePermissionType permissionType, Guid grantedByUserId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string insertPermissionSql = @"
                        INSERT INTO knowledge_base_permission (knowledge_base_id, user_id, permission_type, granted_by_user_id)
                        VALUES (@knowledgeBaseId, @userId, @permissionType, @grantedByUserId)
                        RETURNING id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(insertPermissionSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@permissionType", permissionType.ToString().ToLowerInvariant());
                        command.Parameters.AddWithValue("@grantedByUserId", grantedByUserId);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                throw new InvalidOperationException("Failed to create permission record");
                            }

                            return new KnowledgeBasePermission(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
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
                            throw new InvalidOperationException("Permission already exists for this user and knowledge base.", ex);
                        case "23503": // foreign_key_violation
                            throw new InvalidOperationException("Referenced knowledge base or user does not exist.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeBasePermission?> UpdatePermissionAsync(Guid knowledgeBaseId, Guid userId, KnowledgeBasePermissionType newPermissionType, Guid updatedByUserId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                try
                {
                    string updatePermissionSql = @"
                        UPDATE knowledge_base_permission 
                        SET permission_type = @newPermissionType, granted_by_user_id = @updatedByUserId
                        WHERE knowledge_base_id = @knowledgeBaseId AND user_id = @userId
                        RETURNING id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at";

                    NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                    using (NpgsqlCommand command = new NpgsqlCommand(updatePermissionSql, npgsqlConnection))
                    {
                        command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@newPermissionType", newPermissionType.ToString().ToLowerInvariant());
                        command.Parameters.AddWithValue("@updatedByUserId", updatedByUserId);

                        using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return null;
                            }

                            return new KnowledgeBasePermission(
                                id: reader.GetGuid(reader.GetOrdinal("id")),
                                knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
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
                            throw new InvalidOperationException("Referenced knowledge base or user does not exist.", ex);
                        default:
                            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RevokePermissionAsync(Guid knowledgeBaseId, Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string deletePermissionSql = @"
                    DELETE FROM knowledge_base_permission 
                    WHERE knowledge_base_id = @knowledgeBaseId AND user_id = @userId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(deletePermissionSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                    command.Parameters.AddWithValue("@userId", userId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeBasePermission>> GetPermissionsByKnowledgeBaseAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetPermissionsByParameterAsync("knowledge_base_id", knowledgeBaseId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<List<KnowledgeBasePermission>> GetPermissionsByUserAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetPermissionsByParameterAsync("user_id", userId, connection);
            }
        }

        /// <inheritdoc/>
        public async Task<int> RevokeAllPermissionsForKnowledgeBaseAsync(Guid knowledgeBaseId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                string deletePermissionsSql = @"
                    DELETE FROM knowledge_base_permission 
                    WHERE knowledge_base_id = @knowledgeBaseId";

                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlCommand command = new NpgsqlCommand(deletePermissionsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
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
                using (NpgsqlCommand command = new NpgsqlCommand(deletePermissionsSql, npgsqlConnection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<KnowledgeBasePermissionType?> GetUserPermissionAsync(Guid userId, Guid knowledgeBaseId, IDbConnection connection)
        {
            string getPermissionSql = @"
                SELECT permission_type
                FROM knowledge_base_permission
                WHERE user_id = @userId AND knowledge_base_id = @knowledgeBaseId";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(getPermissionSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                object? result = await command.ExecuteScalarAsync();
                if (result == null)
                    return null;

                return ParsePermissionType((string)result);
            }
        }

        private async Task<bool> IsOwnerAsync(Guid userId, Guid knowledgeBaseId, IDbConnection connection)
        {
            string isOwnerSql = @"
                SELECT COUNT(1)
                FROM knowledge_base
                WHERE id = @knowledgeBaseId AND owner_id = @userId";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(isOwnerSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                command.Parameters.AddWithValue("@userId", userId);
                object? result = await command.ExecuteScalarAsync();
                long count = result == null ? 0 : (long)result;
                return count > 0;
            }
        }

        private async Task<bool> IsPublicAsync(Guid knowledgeBaseId, IDbConnection connection)
        {
            string isPublicSql = @"
                SELECT is_public
                FROM knowledge_base
                WHERE id = @knowledgeBaseId";

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(isPublicSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue("@knowledgeBaseId", knowledgeBaseId);
                object? result = await command.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
        }

        private async Task<List<KnowledgeBasePermission>> GetPermissionsByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getPermissionsSql = $@"
                SELECT id, knowledge_base_id, user_id, permission_type, granted_by_user_id, created_at
                FROM knowledge_base_permission
                WHERE {parameterName} = @{parameterName}
                ORDER BY created_at DESC";

            List<KnowledgeBasePermission> permissions = new List<KnowledgeBasePermission>();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand command = new NpgsqlCommand(getPermissionsSql, npgsqlConnection))
            {
                command.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(new KnowledgeBasePermission(
                            id: reader.GetGuid(reader.GetOrdinal("id")),
                            knowledgeBaseId: reader.GetGuid(reader.GetOrdinal("knowledge_base_id")),
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

        private static KnowledgeBasePermissionType ParsePermissionType(string permissionTypeString)
        {
            return permissionTypeString.ToLowerInvariant() switch
            {
                "read" => KnowledgeBasePermissionType.Read,
                "write" => KnowledgeBasePermissionType.Write,
                "admin" => KnowledgeBasePermissionType.Admin,
                _ => throw new ArgumentException($"Unknown permission type: {permissionTypeString}")
            };
        }

        private static bool HasSufficientPermission(KnowledgeBasePermissionType userPermission, KnowledgeBasePermissionType requiredPermission)
        {
            // Permission hierarchy: Read < Write < Admin
            return userPermission >= requiredPermission;
        }

        private static List<string> GetSufficientPermissionTypes(KnowledgeBasePermissionType minimumPermission)
        {
            return minimumPermission switch
            {
                KnowledgeBasePermissionType.Read => new List<string> { "read", "write", "admin" },
                KnowledgeBasePermissionType.Write => new List<string> { "write", "admin" },
                KnowledgeBasePermissionType.Admin => new List<string> { "admin" },
                _ => throw new ArgumentException($"Unknown permission type: {minimumPermission}")
            };
        }
    }
}

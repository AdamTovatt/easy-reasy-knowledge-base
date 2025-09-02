using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using Npgsql;
using System.Data;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements user data access operations using PostgreSQL.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="connectionFactory">The database connection factory.</param>
        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>
        /// Creates a new user in the database.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="passwordHash">The hashed password.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="roles">The list of role names to assign to the user.</param>
        /// <returns>The created user with populated ID and timestamps.</returns>
        public async Task<User> CreateAsync(string email, string passwordHash, string firstName, string lastName, List<string> roles)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null, empty, or whitespace.", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be null, empty, or whitespace.", nameof(passwordHash));

            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be null, empty, or whitespace.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be null, empty, or whitespace.", nameof(lastName));

            if (roles == null)
                throw new ArgumentException("Roles cannot be null.", nameof(roles));

            // Deduplicate roles to avoid database constraint violations
            List<string> uniqueRoles = roles.Distinct().ToList();

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlTransaction transaction = (NpgsqlTransaction)await npgsqlConnection.BeginTransactionAsync())
                {
                    try
                    {
                        // Insert user
                        string insertUserSql = @"
                            INSERT INTO ""user"" (email, password_hash, first_name, last_name)
                            VALUES (@email, @passwordHash, @firstName, @lastName)
                            RETURNING id, email, password_hash, first_name, last_name, is_active, last_login_at, created_at, updated_at";

                        Guid userId;
                        DateTime createdAt;
                        DateTime updatedAt;
                        bool isActive;
                        DateTime? lastLoginAt;

                        using (NpgsqlCommand userCommand = new(insertUserSql, npgsqlConnection, transaction))
                        {
                            userCommand.Parameters.AddWithValue("@email", email);
                            userCommand.Parameters.AddWithValue("@passwordHash", passwordHash);
                            userCommand.Parameters.AddWithValue("@firstName", firstName);
                            userCommand.Parameters.AddWithValue("@lastName", lastName);

                            using (NpgsqlDataReader userReader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                            {
                                if (!await userReader.ReadAsync())
                                {
                                    throw new InvalidOperationException("Failed to create user");
                                }

                                userId = userReader.GetGuid(userReader.GetOrdinal("id"));
                                createdAt = userReader.GetDateTime(userReader.GetOrdinal("created_at"));
                                updatedAt = userReader.GetDateTime(userReader.GetOrdinal("updated_at"));
                                isActive = userReader.GetBoolean(userReader.GetOrdinal("is_active"));
                                lastLoginAt = userReader.IsDBNull(userReader.GetOrdinal("last_login_at")) ? null : userReader.GetDateTime(userReader.GetOrdinal("last_login_at"));
                            }
                        }

                        // Insert roles
                        if (uniqueRoles.Count > 0)
                        {
                            string insertRolesSql = @"
                                INSERT INTO user_role (user_id, role_name)
                                VALUES (@userId, @roleName)";

                            using (NpgsqlCommand rolesCommand = new(insertRolesSql, npgsqlConnection, transaction))
                            {
                                rolesCommand.Parameters.AddWithValue("@userId", userId);
                                rolesCommand.Parameters.AddWithValue("@roleName", "");

                                foreach (string role in uniqueRoles)
                                {
                                    rolesCommand.Parameters["@roleName"].Value = role;
                                    await rolesCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();

                        return new User(
                            userId,
                            email,
                            passwordHash,
                            firstName,
                            lastName,
                            isActive,
                            lastLoginAt,
                            createdAt,
                            updatedAt,
                            uniqueRoles);
                    }
                    catch (PostgresException ex)
                    {
                        await transaction.RollbackAsync();

                        // Handle specific PostgreSQL constraint violations
                        switch (ex.SqlState)
                        {
                            case "23505": // unique_violation
                                if (ex.ConstraintName?.Contains("user_email") == true || ex.Message.Contains("email"))
                                {
                                    throw new InvalidOperationException("A user with this email address already exists.", ex);
                                }
                                else if (ex.ConstraintName?.Contains("user_role") == true || ex.Message.Contains("role"))
                                {
                                    throw new InvalidOperationException("Duplicate roles are not allowed for the same user.", ex);
                                }
                                else
                                {
                                    throw new InvalidOperationException("A unique constraint violation occurred.", ex);
                                }
                            case "23502": // not_null_violation
                                throw new InvalidOperationException("A required field is missing.", ex);
                            case "23503": // foreign_key_violation
                                throw new InvalidOperationException("A foreign key constraint violation occurred.", ex);
                            case "22001": // string_data_right_truncation
                                throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                            default:
                                throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <returns>The user if found, null otherwise.</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetUserByParameterAsync("id", id, connection);
            }
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, null otherwise.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetUserByParameterAsync("email", email, connection);
            }
        }

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>The updated user.</returns>
        public async Task<User> UpdateAsync(User user)
        {
            // Deduplicate roles to avoid database constraint violations
            List<string> uniqueRoles = user.Roles.Distinct().ToList();
            User userWithUniqueRoles = new(
                user.Id,
                user.Email,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt,
                uniqueRoles);

            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlTransaction transaction = (NpgsqlTransaction)await npgsqlConnection.BeginTransactionAsync())
                {
                    try
                    {
                        // Update user
                        string updateUserSql = @"
                            UPDATE ""user"" 
                            SET email = @email, password_hash = @passwordHash, first_name = @firstName, 
                                last_name = @lastName, is_active = @isActive, last_login_at = @lastLoginAt
                            WHERE id = @id
                            RETURNING id, email, password_hash, first_name, last_name, is_active, last_login_at, created_at, updated_at";

                        DateTime updatedAt;

                        using (NpgsqlCommand userCommand = new(updateUserSql, npgsqlConnection, transaction))
                        {
                            userCommand.Parameters.AddWithValue("@id", userWithUniqueRoles.Id);
                            userCommand.Parameters.AddWithValue("@email", userWithUniqueRoles.Email);
                            userCommand.Parameters.AddWithValue("@passwordHash", userWithUniqueRoles.PasswordHash);
                            userCommand.Parameters.AddWithValue("@firstName", userWithUniqueRoles.FirstName);
                            userCommand.Parameters.AddWithValue("@lastName", userWithUniqueRoles.LastName);
                            userCommand.Parameters.AddWithValue("@isActive", userWithUniqueRoles.IsActive);
                            userCommand.Parameters.AddWithValue("@lastLoginAt", (object?)userWithUniqueRoles.LastLoginAt ?? DBNull.Value);

                            using (NpgsqlDataReader userReader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                            {
                                if (!await userReader.ReadAsync())
                                {
                                    throw new InvalidOperationException($"User with ID {userWithUniqueRoles.Id} not found");
                                }

                                updatedAt = userReader.GetDateTime(userReader.GetOrdinal("updated_at"));
                            }
                        }

                        // Delete existing roles
                        string deleteRolesSql = @"
                            DELETE FROM user_role 
                            WHERE user_id = @userId";

                        using (NpgsqlCommand deleteRolesCommand = new(deleteRolesSql, npgsqlConnection, transaction))
                        {
                            deleteRolesCommand.Parameters.AddWithValue("@userId", userWithUniqueRoles.Id);
                            await deleteRolesCommand.ExecuteNonQueryAsync();
                        }

                        // Insert new roles
                        if (userWithUniqueRoles.Roles.Count > 0)
                        {
                            string insertRolesSql = @"
                                INSERT INTO user_role (user_id, role_name)
                                VALUES (@userId, @roleName)";

                            using (NpgsqlCommand rolesCommand = new(insertRolesSql, npgsqlConnection, transaction))
                            {
                                rolesCommand.Parameters.AddWithValue("@userId", userWithUniqueRoles.Id);
                                rolesCommand.Parameters.AddWithValue("@roleName", "");

                                foreach (string role in userWithUniqueRoles.Roles)
                                {
                                    rolesCommand.Parameters["@roleName"].Value = role;
                                    await rolesCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();

                        // Return updated user with new updated_at timestamp
                        return new User(
                            userWithUniqueRoles.Id,
                            userWithUniqueRoles.Email,
                            userWithUniqueRoles.PasswordHash,
                            userWithUniqueRoles.FirstName,
                            userWithUniqueRoles.LastName,
                            userWithUniqueRoles.IsActive,
                            userWithUniqueRoles.LastLoginAt,
                            userWithUniqueRoles.CreatedAt,
                            updatedAt,
                            userWithUniqueRoles.Roles);
                    }
                    catch (PostgresException ex)
                    {
                        await transaction.RollbackAsync();

                        // Handle specific PostgreSQL constraint violations
                        switch (ex.SqlState)
                        {
                            case "23505": // unique_violation
                                if (ex.ConstraintName?.Contains("user_email") == true || ex.Message.Contains("email"))
                                {
                                    throw new InvalidOperationException("A user with this email address already exists.", ex);
                                }
                                else if (ex.ConstraintName?.Contains("user_role") == true || ex.Message.Contains("role"))
                                {
                                    throw new InvalidOperationException("Duplicate roles are not allowed for the same user.", ex);
                                }
                                else
                                {
                                    throw new InvalidOperationException("A unique constraint violation occurred.", ex);
                                }
                            case "23502": // not_null_violation
                                throw new InvalidOperationException("A required field is missing.", ex);
                            case "23503": // foreign_key_violation
                                throw new InvalidOperationException("A foreign key constraint violation occurred.", ex);
                            case "22001": // string_data_right_truncation
                                throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                            default:
                                throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes a user from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// <returns>True if the user was deleted, false if not found.</returns>
        public async Task<bool> DeleteAsync(Guid id)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                using (NpgsqlTransaction transaction = (NpgsqlTransaction)await npgsqlConnection.BeginTransactionAsync())
                {
                    try
                    {
                        // Delete user roles first (due to foreign key constraint)
                        string deleteRolesSql = @"
                            DELETE FROM user_role 
                            WHERE user_id = @userId";

                        using (NpgsqlCommand deleteRolesCommand = new(deleteRolesSql, npgsqlConnection, transaction))
                        {
                            deleteRolesCommand.Parameters.AddWithValue("@userId", id);
                            await deleteRolesCommand.ExecuteNonQueryAsync();
                        }

                        // Delete user
                        string deleteUserSql = @"
                            DELETE FROM ""user"" 
                            WHERE id = @id";

                        using (NpgsqlCommand deleteUserCommand = new(deleteUserSql, npgsqlConnection, transaction))
                        {
                            deleteUserCommand.Parameters.AddWithValue("@id", id);
                            int rowsAffected = await deleteUserCommand.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (PostgresException ex)
                    {
                        await transaction.RollbackAsync();

                        // Handle specific PostgreSQL constraint violations
                        switch (ex.SqlState)
                        {
                            case "23503": // foreign_key_violation
                                throw new InvalidOperationException("Cannot delete user due to existing references.", ex);
                            case "23502": // not_null_violation
                                throw new InvalidOperationException("A required field is missing.", ex);
                            case "22001": // string_data_right_truncation
                                throw new InvalidOperationException("One or more fields exceed the maximum allowed length.", ex);
                            default:
                                throw new InvalidOperationException($"Database error: {ex.Message}", ex);
                        }
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        /// <returns>A list of all users.</returns>
        public async Task<List<User>> GetAllAsync()
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                string getAllUsersSql = @"
                    SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.is_active, u.last_login_at, u.created_at, u.updated_at, ur.role_name
                    FROM ""user"" u
                    LEFT JOIN user_role ur ON u.id = ur.user_id
                    ORDER BY u.created_at DESC";

                Dictionary<Guid, User> userDict = new();

                using (NpgsqlCommand userCommand = new(getAllUsersSql, npgsqlConnection))
                {
                    using (NpgsqlDataReader reader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                            string email = reader.GetString(reader.GetOrdinal("email"));
                            string passwordHash = reader.GetString(reader.GetOrdinal("password_hash"));
                            string firstName = reader.GetString(reader.GetOrdinal("first_name"));
                            string lastName = reader.GetString(reader.GetOrdinal("last_name"));
                            bool isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
                            DateTime? lastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_at"));
                            DateTime createdAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                            DateTime updatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));

                            if (!userDict.ContainsKey(id))
                            {
                                userDict[id] = new User(
                                    id,
                                    email,
                                    passwordHash,
                                    firstName,
                                    lastName,
                                    isActive,
                                    lastLoginAt,
                                    createdAt,
                                    updatedAt,
                                    new List<string>());
                            }

                            // Add role if it exists (role_name will be null for users without roles)
                            if (!reader.IsDBNull(reader.GetOrdinal("role_name")))
                            {
                                string roleName = reader.GetString(reader.GetOrdinal("role_name"));
                                userDict[id].Roles.Add(roleName);
                            }
                        }
                    }
                }

                return userDict.Values.ToList();
            }
        }

        /// <summary>
        /// Checks if a user with the specified email address exists.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if a user with the email exists, false otherwise.</returns>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                string existsSql = @"
                    SELECT COUNT(1)
                    FROM ""user""
                    WHERE email = @email";

                using (NpgsqlCommand existsCommand = new(existsSql, npgsqlConnection))
                {
                    existsCommand.Parameters.AddWithValue("@email", email);
                    object? result = await existsCommand.ExecuteScalarAsync();
                    long count = result == null ? 0 : (long)result;
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// Updates the last login timestamp for a user.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="lastLoginAt">The timestamp of the last login.</param>
        /// <returns>True if the update was successful, false if user not found.</returns>
        public async Task<bool> UpdateLastLoginAsync(Guid id, DateTime lastLoginAt)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
                string updateLastLoginSql = @"
                    UPDATE ""user"" 
                    SET last_login_at = @lastLoginAt
                    WHERE id = @id";

                using (NpgsqlCommand updateCommand = new(updateLastLoginSql, npgsqlConnection))
                {
                    updateCommand.Parameters.AddWithValue("@id", id);
                    updateCommand.Parameters.AddWithValue("@lastLoginAt", lastLoginAt);
                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// Retrieves the list of role names for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of role names assigned to the user.</returns>
        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            using (IDbConnection connection = await _connectionFactory.CreateOpenConnectionAsync())
            {
                return await GetUserRolesAsync(userId, connection);
            }
        }

        /// <summary>
        /// Retrieves a user by a specific parameter using an existing connection.
        /// </summary>
        /// <typeparam name="T">The type of the parameter value.</typeparam>
        /// <param name="parameterName">The name of the parameter (e.g., "id", "email").</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <param name="connection">The open database connection to use.</param>
        /// <returns>The user if found, null otherwise.</returns>
        private async Task<User?> GetUserByParameterAsync<T>(string parameterName, T parameterValue, IDbConnection connection)
        {
            string getUserSql = $@"
                SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.is_active, u.last_login_at, u.created_at, u.updated_at, ur.role_name
                FROM ""user"" u
                LEFT JOIN user_role ur ON u.id = ur.user_id
                WHERE u.{parameterName} = @{parameterName}";

            Guid id = Guid.Empty;
            string email = string.Empty;
            string passwordHash = string.Empty;
            string firstName = string.Empty;
            string lastName = string.Empty;
            bool isActive = false;
            DateTime? lastLoginAt = null;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;
            List<string> roles = new();

            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand userCommand = new(getUserSql, npgsqlConnection))
            {
                userCommand.Parameters.AddWithValue($"@{parameterName}", (object?)parameterValue ?? DBNull.Value);

                using (NpgsqlDataReader reader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                {
                    bool hasUser = false;
                    while (await reader.ReadAsync())
                    {
                        if (!hasUser)
                        {
                            id = reader.GetGuid(reader.GetOrdinal("id"));
                            email = reader.GetString(reader.GetOrdinal("email"));
                            passwordHash = reader.GetString(reader.GetOrdinal("password_hash"));
                            firstName = reader.GetString(reader.GetOrdinal("first_name"));
                            lastName = reader.GetString(reader.GetOrdinal("last_name"));
                            isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
                            lastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login_at"));
                            createdAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
                            updatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"));
                            hasUser = true;
                        }

                        // Add role if it exists (role_name will be null for users without roles)
                        if (!reader.IsDBNull(reader.GetOrdinal("role_name")))
                        {
                            string roleName = reader.GetString(reader.GetOrdinal("role_name"));
                            roles.Add(roleName);
                        }
                    }

                    if (!hasUser)
                    {
                        return null;
                    }
                }
            }

            return new User(
                id,
                email,
                passwordHash,
                firstName,
                lastName,
                isActive,
                lastLoginAt,
                createdAt,
                updatedAt,
                roles);
        }

        /// <summary>
        /// Retrieves the list of role names for a specific user using an existing connection.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="connection">The open database connection to use.</param>
        /// <returns>A list of role names assigned to the user.</returns>
        private async Task<List<string>> GetUserRolesAsync(Guid userId, IDbConnection connection)
        {
            string getRolesSql = @"
                SELECT role_name
                FROM user_role
                WHERE user_id = @userId";

            List<string> roles = new();
            NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;
            using (NpgsqlCommand rolesCommand = new(getRolesSql, npgsqlConnection))
            {
                rolesCommand.Parameters.AddWithValue("@userId", userId);

                using (NpgsqlDataReader rolesReader = (NpgsqlDataReader)await rolesCommand.ExecuteReaderAsync())
                {
                    while (await rolesReader.ReadAsync())
                    {
                        roles.Add(rolesReader.GetString(rolesReader.GetOrdinal("role_name")));
                    }
                }
            }

            return roles;
        }
    }
}

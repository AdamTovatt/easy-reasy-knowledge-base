using EasyReasy.KnowledgeBase.Web.Server.Models;
using Npgsql;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Implements user data access operations using PostgreSQL.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="connectionString">The PostgreSQL connection string.</param>
        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
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
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (NpgsqlTransaction transaction = (NpgsqlTransaction)await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Insert user
                        string insertUserSql = @"
                            INSERT INTO user (email, password_hash, first_name, last_name)
                            VALUES (@email, @passwordHash, @firstName, @lastName)
                            RETURNING id, email, password_hash, first_name, last_name, is_active, last_login_at, created_at, updated_at";

                        Guid userId;
                        DateTime createdAt;
                        DateTime updatedAt;
                        bool isActive;
                        DateTime? lastLoginAt;

                        using (NpgsqlCommand userCommand = new NpgsqlCommand(insertUserSql, connection, transaction))
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
                        if (roles.Count > 0)
                        {
                            string insertRolesSql = @"
                                INSERT INTO user_role (user_id, role_name)
                                VALUES (@userId, @roleName)";

                            using (NpgsqlCommand rolesCommand = new NpgsqlCommand(insertRolesSql, connection, transaction))
                            {
                                rolesCommand.Parameters.AddWithValue("@userId", userId);

                                foreach (string role in roles)
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
                            roles);
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
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
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
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
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
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (NpgsqlTransaction transaction = (NpgsqlTransaction)await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Update user
                        string updateUserSql = @"
                            UPDATE user 
                            SET email = @email, password_hash = @passwordHash, first_name = @firstName, 
                                last_name = @lastName, is_active = @isActive, last_login_at = @lastLoginAt
                            WHERE id = @id
                            RETURNING id, email, password_hash, first_name, last_name, is_active, last_login_at, created_at, updated_at";

                        DateTime updatedAt;

                        using (NpgsqlCommand userCommand = new NpgsqlCommand(updateUserSql, connection, transaction))
                        {
                            userCommand.Parameters.AddWithValue("@id", user.Id);
                            userCommand.Parameters.AddWithValue("@email", user.Email);
                            userCommand.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
                            userCommand.Parameters.AddWithValue("@firstName", user.FirstName);
                            userCommand.Parameters.AddWithValue("@lastName", user.LastName);
                            userCommand.Parameters.AddWithValue("@isActive", user.IsActive);
                            userCommand.Parameters.AddWithValue("@lastLoginAt", (object?)user.LastLoginAt ?? DBNull.Value);

                            using (NpgsqlDataReader userReader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                            {
                                if (!await userReader.ReadAsync())
                                {
                                    throw new InvalidOperationException($"User with ID {user.Id} not found");
                                }

                                updatedAt = userReader.GetDateTime(userReader.GetOrdinal("updated_at"));
                            }
                        }

                        // Delete existing roles
                        string deleteRolesSql = @"
                            DELETE FROM user_role 
                            WHERE user_id = @userId";

                        using (NpgsqlCommand deleteRolesCommand = new NpgsqlCommand(deleteRolesSql, connection, transaction))
                        {
                            deleteRolesCommand.Parameters.AddWithValue("@userId", user.Id);
                            await deleteRolesCommand.ExecuteNonQueryAsync();
                        }

                        // Insert new roles
                        if (user.Roles.Count > 0)
                        {
                            string insertRolesSql = @"
                                INSERT INTO user_role (user_id, role_name)
                                VALUES (@userId, @roleName)";

                            using (NpgsqlCommand rolesCommand = new NpgsqlCommand(insertRolesSql, connection, transaction))
                            {
                                rolesCommand.Parameters.AddWithValue("@userId", user.Id);

                                foreach (string role in user.Roles)
                                {
                                    rolesCommand.Parameters["@roleName"].Value = role;
                                    await rolesCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaction.CommitAsync();

                        // Return updated user with new updated_at timestamp
                        return new User(
                            user.Id,
                            user.Email,
                            user.PasswordHash,
                            user.FirstName,
                            user.LastName,
                            user.IsActive,
                            user.LastLoginAt,
                            user.CreatedAt,
                            updatedAt,
                            user.Roles);
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
        public Task<bool> DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        /// <returns>A list of all users.</returns>
        public Task<List<User>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if a user with the specified email address exists.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if a user with the email exists, false otherwise.</returns>
        public Task<bool> ExistsByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the last login timestamp for a user.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="lastLoginAt">The timestamp of the last login.</param>
        /// <returns>True if the update was successful, false if user not found.</returns>
        public Task<bool> UpdateLastLoginAsync(Guid id, DateTime lastLoginAt)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the list of role names for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of role names assigned to the user.</returns>
        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
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
        private async Task<User?> GetUserByParameterAsync<T>(string parameterName, T parameterValue, NpgsqlConnection connection)
        {
            string getUserSql = $@"
                SELECT id, email, password_hash, first_name, last_name, is_active, last_login_at, created_at, updated_at
                FROM user
                WHERE {parameterName} = @{parameterName}";

            Guid id;
            string email;
            string passwordHash;
            string firstName;
            string lastName;
            bool isActive;
            DateTime? lastLoginAt;
            DateTime createdAt;
            DateTime updatedAt;

            using (NpgsqlCommand userCommand = new NpgsqlCommand(getUserSql, connection))
            {
                userCommand.Parameters.AddWithValue($"@{parameterName}", parameterValue);

                using (NpgsqlDataReader userReader = (NpgsqlDataReader)await userCommand.ExecuteReaderAsync())
                {
                    if (!await userReader.ReadAsync())
                    {
                        return null;
                    }

                    id = userReader.GetGuid(userReader.GetOrdinal("id"));
                    email = userReader.GetString(userReader.GetOrdinal("email"));
                    passwordHash = userReader.GetString(userReader.GetOrdinal("password_hash"));
                    firstName = userReader.GetString(userReader.GetOrdinal("first_name"));
                    lastName = userReader.GetString(userReader.GetOrdinal("last_name"));
                    isActive = userReader.GetBoolean(userReader.GetOrdinal("is_active"));
                    lastLoginAt = userReader.IsDBNull(userReader.GetOrdinal("last_login_at")) ? null : userReader.GetDateTime(userReader.GetOrdinal("last_login_at"));
                    createdAt = userReader.GetDateTime(userReader.GetOrdinal("created_at"));
                    updatedAt = userReader.GetDateTime(userReader.GetOrdinal("updated_at"));
                }
            }

            // Get user roles
            List<string> roles = await GetUserRolesAsync(id, connection);

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
        private async Task<List<string>> GetUserRolesAsync(Guid userId, NpgsqlConnection connection)
        {
            string getRolesSql = @"
                SELECT role_name
                FROM user_role
                WHERE user_id = @userId";

            List<string> roles = new List<string>();
            using (NpgsqlCommand rolesCommand = new NpgsqlCommand(getRolesSql, connection))
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

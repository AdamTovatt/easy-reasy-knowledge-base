using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for user data access operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Creates a new user in the database.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="passwordHash">The hashed password.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="roles">The list of role names to assign to the user.</param>
        /// <returns>The created user with populated ID and timestamps.</returns>
        Task<User> CreateAsync(string email, string passwordHash, string firstName, string lastName, List<string> roles);

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>The updated user.</returns>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Deletes a user from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// <returns>True if the user was deleted, false if not found.</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        /// <returns>A list of all users.</returns>
        Task<List<User>> GetAllAsync();

        /// <summary>
        /// Checks if a user with the specified email address exists.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if a user with the email exists, false otherwise.</returns>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>
        /// Updates the last login timestamp for a user.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="lastLoginAt">The timestamp of the last login.</param>
        /// <returns>True if the update was successful, false if user not found.</returns>
        Task<bool> UpdateLastLoginAsync(Guid id, DateTime lastLoginAt);

        /// <summary>
        /// Retrieves the list of role names for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of role names assigned to the user.</returns>
        Task<List<string>> GetUserRolesAsync(Guid userId);
    }
}

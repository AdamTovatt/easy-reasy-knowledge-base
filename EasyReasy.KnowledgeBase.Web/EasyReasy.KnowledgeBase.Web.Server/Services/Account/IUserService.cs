using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Account
{
    /// <summary>
    /// Service for handling user-related business logic.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Validates user credentials and returns the user if valid.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The plain text password.</param>
        /// <returns>The user if credentials are valid, null otherwise.</returns>
        Task<User?> ValidateCredentialsAsync(string email, string password);

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The plain text password.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="roles">The list of role names to assign to the user.</param>
        /// <returns>The created user.</returns>
        Task<User> CreateUserAsync(string email, string password, string firstName, string lastName, List<string> roles);

        /// <summary>
        /// Updates the last login timestamp for a user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <returns>True if the update was successful, false if user not found.</returns>
        Task<bool> UpdateLastLoginAsync(Guid userId);

        /// <summary>
        /// Checks if a user with the specified email address exists.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if a user with the email exists, false otherwise.</returns>
        Task<bool> UserExistsAsync(string email);

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetUserByIdAsync(Guid id);
    }
}

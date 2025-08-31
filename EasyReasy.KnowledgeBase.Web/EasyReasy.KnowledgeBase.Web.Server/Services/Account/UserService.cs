using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.Auth;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Account
{
    /// <summary>
    /// Implementation of user-related business logic.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userRepository">The user repository for data access.</param>
        /// <param name="passwordHasher">The password hasher for secure password operations.</param>
        public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        /// <inheritdoc/>
        public async Task<User?> ValidateCredentialsAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            User? user = await _userRepository.GetByEmailAsync(email);
            if (user == null || !user.IsActive)
                return null;

            bool isPasswordValid = _passwordHasher.ValidatePassword(password, user.PasswordHash, email);
            if (!isPasswordValid)
                return null;

            return user;
        }

        /// <inheritdoc/>
        public async Task<User> CreateUserAsync(string email, string password, string firstName, string lastName, List<string> roles)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));

            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name is required.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name is required.", nameof(lastName));

            // Check if user already exists
            bool userExists = await _userRepository.ExistsByEmailAsync(email);
            if (userExists)
                throw new InvalidOperationException($"A user with email '{email}' already exists.");

            // Hash the password
            string passwordHash = _passwordHasher.HashPassword(password, email);

            // Create the user
            User user = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);
            return user;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            return await _userRepository.UpdateLastLoginAsync(userId, DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public async Task<bool> UserExistsAsync(string email)
        {
            return await _userRepository.ExistsByEmailAsync(email);
        }

        /// <inheritdoc/>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        /// <inheritdoc/>
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
        }
    }
}

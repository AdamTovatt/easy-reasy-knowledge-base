using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IUserRepository for testing purposes.
    /// </summary>
    public class MockUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _users = new Dictionary<Guid, User>();
        private readonly Dictionary<string, User> _usersByEmail = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, List<string>> _userRoles = new Dictionary<Guid, List<string>>();

        public Task<User> CreateAsync(string email, string passwordHash, string firstName, string lastName, List<string> roles)
        {
            if (_usersByEmail.ContainsKey(email))
            {
                throw new InvalidOperationException("A user with this email address already exists.");
            }

            Guid id = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;

            User user = new User(
                id,
                email,
                passwordHash,
                firstName,
                lastName,
                true, // isActive
                null, // lastLoginAt
                now,  // createdAt
                now,  // updatedAt
                roles.Distinct().ToList());

            _users[id] = user;
            _usersByEmail[email] = user;
            _userRoles[id] = roles.Distinct().ToList();

            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            _users.TryGetValue(id, out User? user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email, out User? user);
            return Task.FromResult(user);
        }

        public Task<User> UpdateAsync(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                throw new InvalidOperationException($"User with ID {user.Id} not found");
            }

            // Check for email conflicts (excluding the current user)
            if (_usersByEmail.TryGetValue(user.Email, out User? existingUser) && existingUser.Id != user.Id)
            {
                throw new InvalidOperationException("A user with this email address already exists.");
            }

            // Remove old email mapping if email changed
            User oldUser = _users[user.Id];
            if (!string.Equals(oldUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                _usersByEmail.Remove(oldUser.Email);
            }

            // Update user with new timestamp
            User updatedUser = new User(
                user.Id,
                user.Email,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.LastLoginAt,
                user.CreatedAt,
                DateTime.UtcNow, // updatedAt
                user.Roles.Distinct().ToList());

            _users[user.Id] = updatedUser;
            _usersByEmail[user.Email] = updatedUser;
            _userRoles[user.Id] = user.Roles.Distinct().ToList();

            return Task.FromResult(updatedUser);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            if (!_users.ContainsKey(id))
            {
                return Task.FromResult(false);
            }

            User user = _users[id];
            _users.Remove(id);
            _usersByEmail.Remove(user.Email);
            _userRoles.Remove(id);

            return Task.FromResult(true);
        }

        public Task<List<User>> GetAllAsync()
        {
            return Task.FromResult(_users.Values.ToList());
        }

        public Task<bool> ExistsByEmailAsync(string email)
        {
            return Task.FromResult(_usersByEmail.ContainsKey(email));
        }

        public Task<bool> UpdateLastLoginAsync(Guid id, DateTime lastLoginAt)
        {
            if (!_users.ContainsKey(id))
            {
                return Task.FromResult(false);
            }

            User oldUser = _users[id];
            User updatedUser = new User(
                oldUser.Id,
                oldUser.Email,
                oldUser.PasswordHash,
                oldUser.FirstName,
                oldUser.LastName,
                oldUser.IsActive,
                lastLoginAt,
                oldUser.CreatedAt,
                oldUser.UpdatedAt,
                oldUser.Roles);

            _users[id] = updatedUser;
            _usersByEmail[oldUser.Email] = updatedUser;

            return Task.FromResult(true);
        }

        public Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            _userRoles.TryGetValue(userId, out List<string>? roles);
            return Task.FromResult(roles ?? new List<string>());
        }

        /// <summary>
        /// Clears all mock data for test cleanup.
        /// </summary>
        public void Clear()
        {
            _users.Clear();
            _usersByEmail.Clear();
            _userRoles.Clear();
        }

        /// <summary>
        /// Adds a user directly to the mock repository for test setup.
        /// </summary>
        /// <param name="user">The user to add.</param>
        public void AddUser(User user)
        {
            _users[user.Id] = user;
            _usersByEmail[user.Email] = user;
            _userRoles[user.Id] = user.Roles.ToList();
        }
    }
}

using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;
using EasyReasy.EnvironmentVariables;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Repositories
{
    /// <summary>
    /// Integration tests for the UserRepository using a real PostgreSQL database.
    /// </summary>
    [TestClass]
    public class UserRepositoryTests : DatabaseTestBase
    {
        private static IUserRepository _userRepository = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseTestEnvironment();
            _userRepository = new UserRepository(_connectionFactory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test data after each test
            string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);
        }

        [TestMethod]
        public async Task CreateAsync_ValidUser_CreatesUserSuccessfully()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user", "admin" };

            // Act
            User createdUser = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreNotEqual(Guid.Empty, createdUser.Id);
            Assert.AreEqual(email, createdUser.Email);
            Assert.AreEqual(passwordHash, createdUser.PasswordHash);
            Assert.AreEqual(firstName, createdUser.FirstName);
            Assert.AreEqual(lastName, createdUser.LastName);
            Assert.IsTrue(createdUser.IsActive);
            Assert.IsNull(createdUser.LastLoginAt);
            Assert.AreEqual(2, createdUser.Roles.Count);
            Assert.IsTrue(createdUser.Roles.Contains("user"));
            Assert.IsTrue(createdUser.Roles.Contains("admin"));
            Assert.IsTrue(createdUser.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(createdUser.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [TestMethod]
        public async Task CreateAsync_UserWithNoRoles_CreatesUserSuccessfully()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string>();

            // Act
            User createdUser = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(0, createdUser.Roles.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            User createdUser = await CreateTestUser();

            // Act
            User? retrievedUser = await _userRepository.GetByIdAsync(createdUser.Id);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(createdUser.Id, retrievedUser.Id);
            Assert.AreEqual(createdUser.Email, retrievedUser.Email);
            Assert.AreEqual(createdUser.FirstName, retrievedUser.FirstName);
            Assert.AreEqual(createdUser.LastName, retrievedUser.LastName);
            Assert.AreEqual(createdUser.Roles.Count, retrievedUser.Roles.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            User? retrievedUser = await _userRepository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.IsNull(retrievedUser);
        }

        [TestMethod]
        public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            User createdUser = await CreateTestUser();

            // Act
            User? retrievedUser = await _userRepository.GetByEmailAsync(createdUser.Email);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(createdUser.Id, retrievedUser.Id);
            Assert.AreEqual(createdUser.Email, retrievedUser.Email);
        }

        [TestMethod]
        public async Task GetByEmailAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange
            string nonExistentEmail = "nonexistent@example.com";

            // Act
            User? retrievedUser = await _userRepository.GetByEmailAsync(nonExistentEmail);

            // Assert
            Assert.IsNull(retrievedUser);
        }

        [TestMethod]
        public async Task UpdateAsync_ExistingUser_UpdatesUserSuccessfully()
        {
            // Arrange
            User originalUser = await CreateTestUser();
            User updatedUser = new User(
                originalUser.Id,
                "updated@example.com",
                "new_password_hash",
                "Jane",
                "Smith",
                false,
                DateTime.UtcNow,
                originalUser.CreatedAt,
                originalUser.UpdatedAt,
                new List<string> { "moderator" });

            // Act
            User result = await _userRepository.UpdateAsync(updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updatedUser.Id, result.Id);
            Assert.AreEqual(updatedUser.Email, result.Email);
            Assert.AreEqual(updatedUser.PasswordHash, result.PasswordHash);
            Assert.AreEqual(updatedUser.FirstName, result.FirstName);
            Assert.AreEqual(updatedUser.LastName, result.LastName);
            Assert.AreEqual(updatedUser.IsActive, result.IsActive);
            Assert.AreEqual(updatedUser.LastLoginAt, result.LastLoginAt);
            Assert.AreEqual(1, result.Roles.Count);
            Assert.IsTrue(result.Roles.Contains("moderator"));
            Assert.IsTrue(result.UpdatedAt > originalUser.UpdatedAt);
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentUser_ThrowsException()
        {
            // Arrange
            User nonExistentUser = new User(
                Guid.NewGuid(),
                "nonexistent@example.com",
                "password_hash",
                "John",
                "Doe",
                true,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow,
                new List<string>());

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _userRepository.UpdateAsync(nonExistentUser));
        }

        [TestMethod]
        public async Task DeleteAsync_ExistingUser_DeletesUserSuccessfully()
        {
            // Arrange
            User createdUser = await CreateTestUser();

            // Act
            bool result = await _userRepository.DeleteAsync(createdUser.Id);

            // Assert
            Assert.IsTrue(result);

            // Verify user is deleted
            User? deletedUser = await _userRepository.GetByIdAsync(createdUser.Id);
            Assert.IsNull(deletedUser);
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _userRepository.DeleteAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetAllAsync_NoUsers_ReturnsEmptyList()
        {
            // Act
            List<User> users = await _userRepository.GetAllAsync();

            // Assert
            Assert.IsNotNull(users);
            Assert.AreEqual(0, users.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_MultipleUsers_ReturnsAllUsers()
        {
            // Arrange
            User user1 = await CreateTestUser("user1@example.com", "User", "One", new List<string> { "user" });
            User user2 = await CreateTestUser("user2@example.com", "User", "Two", new List<string> { "user" });

            // Act
            List<User> users = await _userRepository.GetAllAsync();

            // Assert
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.Any(u => u.Id == user1.Id));
            Assert.IsTrue(users.Any(u => u.Id == user2.Id));
        }

        [TestMethod]
        public async Task ExistsByEmailAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            User createdUser = await CreateTestUser();

            // Act
            bool exists = await _userRepository.ExistsByEmailAsync(createdUser.Email);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ExistsByEmailAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            string nonExistentEmail = "nonexistent@example.com";

            // Act
            bool exists = await _userRepository.ExistsByEmailAsync(nonExistentEmail);

            // Assert
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public async Task UpdateLastLoginAsync_ExistingUser_UpdatesLastLoginSuccessfully()
        {
            // Arrange
            User createdUser = await CreateTestUser();
            DateTime lastLoginAt = DateTime.UtcNow;

            // Act
            bool result = await _userRepository.UpdateLastLoginAsync(createdUser.Id, lastLoginAt);

            // Assert
            Assert.IsTrue(result);

            // Verify the update
            User? updatedUser = await _userRepository.GetByIdAsync(createdUser.Id);
            Assert.IsNotNull(updatedUser);
            Assert.IsNotNull(updatedUser.LastLoginAt);
            // Compare with tolerance for potential precision differences in database storage
            TimeSpan difference = lastLoginAt - updatedUser.LastLoginAt.Value;
            Assert.IsTrue(Math.Abs(difference.TotalMilliseconds) < 100, 
                $"DateTime difference too large: {difference.TotalMilliseconds}ms. Expected: {lastLoginAt}, Actual: {updatedUser.LastLoginAt}");
        }

        [TestMethod]
        public async Task UpdateLastLoginAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();
            DateTime lastLoginAt = DateTime.UtcNow;

            // Act
            bool result = await _userRepository.UpdateLastLoginAsync(nonExistentId, lastLoginAt);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetUserRolesAsync_UserWithRoles_ReturnsRoles()
        {
            // Arrange
            User createdUser = await CreateTestUser();

            // Act
            List<string> roles = await _userRepository.GetUserRolesAsync(createdUser.Id);

            // Assert
            Assert.IsNotNull(roles);
            Assert.AreEqual(2, roles.Count);
            Assert.IsTrue(roles.Contains("user"));
            Assert.IsTrue(roles.Contains("admin"));
        }

        [TestMethod]
        public async Task GetUserRolesAsync_UserWithoutRoles_ReturnsEmptyList()
        {
            // Arrange
            User createdUser = await CreateTestUser("test@example.com", "John", "Doe", new List<string>());

            // Act
            List<string> roles = await _userRepository.GetUserRolesAsync(createdUser.Id);

            // Assert
            Assert.IsNotNull(roles);
            Assert.AreEqual(0, roles.Count);
        }

        [TestMethod]
        public async Task GetUserRolesAsync_NonExistentUser_ReturnsEmptyList()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            List<string> roles = await _userRepository.GetUserRolesAsync(nonExistentId);

            // Assert
            Assert.IsNotNull(roles);
            Assert.AreEqual(0, roles.Count);
        }

        /// <summary>
        /// Helper method to create a test user with default values.
        /// </summary>
        private async Task<User> CreateTestUser()
        {
            return await CreateTestUser("test@example.com", "John", "Doe", new List<string> { "user", "admin" });
        }

        /// <summary>
        /// Helper method to create a test user with custom values.
        /// </summary>
        private async Task<User> CreateTestUser(string email, string firstName, string lastName, List<string> roles)
        {
            string passwordHash = "hashed_password_123";
            return await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);
        }

        #region Input Validation Tests

        [TestMethod]
        public async Task CreateAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            string email = "";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_NullEmail_ThrowsArgumentException()
        {
            // Arrange
            string? email = null;
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email!, passwordHash, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_EmptyPasswordHash_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_NullPasswordHash_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string? passwordHash = null;
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash!, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_EmptyFirstName_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_EmptyLastName_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateAsync_NullRoles_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string>? roles = null;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles!));
        }

        #endregion

        #region Duplicate Email Tests

        [TestMethod]
        public async Task CreateAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            string email = "duplicate@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Create first user
            await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Act & Assert - Try to create second user with same email
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _userRepository.CreateAsync(email, "different_hash", "Jane", "Smith", roles));
        }

        [TestMethod]
        public async Task UpdateAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            User user1 = await CreateTestUser("user1@example.com", "John", "Doe", new List<string> { "user" });
            User user2 = await CreateTestUser("user2@example.com", "Jane", "Smith", new List<string> { "user" });

            // Update user2 to have the same email as user1
            User updatedUser2 = new User(
                user2.Id,
                user1.Email, // Same email as user1
                user2.PasswordHash,
                user2.FirstName,
                user2.LastName,
                user2.IsActive,
                user2.LastLoginAt,
                user2.CreatedAt,
                user2.UpdatedAt,
                user2.Roles);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _userRepository.UpdateAsync(updatedUser2));
        }

        #endregion

        #region Edge Cases for Roles

        [TestMethod]
        public async Task CreateAsync_UserWithDuplicateRoles_HandlesCorrectly()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user", "user", "admin" }; // Duplicate "user"

            // Act
            User createdUser = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(2, createdUser.Roles.Count); // Should deduplicate
            Assert.IsTrue(createdUser.Roles.Contains("user"));
            Assert.IsTrue(createdUser.Roles.Contains("admin"));
        }

        [TestMethod]
        public async Task UpdateAsync_UserWithDuplicateRoles_HandlesCorrectly()
        {
            // Arrange
            User originalUser = await CreateTestUser();
            List<string> duplicateRoles = new List<string> { "user", "user", "admin", "admin" };

            User updatedUser = new User(
                originalUser.Id,
                originalUser.Email,
                originalUser.PasswordHash,
                originalUser.FirstName,
                originalUser.LastName,
                originalUser.IsActive,
                originalUser.LastLoginAt,
                originalUser.CreatedAt,
                originalUser.UpdatedAt,
                duplicateRoles);

            // Act
            User result = await _userRepository.UpdateAsync(updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Roles.Count); // Should deduplicate
            Assert.IsTrue(result.Roles.Contains("user"));
            Assert.IsTrue(result.Roles.Contains("admin"));
        }

        [TestMethod]
        public async Task CreateAsync_UserWithVeryLongRoleName_HandlesCorrectly()
        {
            // Arrange
            string email = "test@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "John";
            string lastName = "Doe";
            string longRoleName = new string('a', 45); // 45 character role name (within 50 char limit)
            List<string> roles = new List<string> { longRoleName };

            // Act
            User createdUser = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(1, createdUser.Roles.Count);
            Assert.AreEqual(longRoleName, createdUser.Roles[0]);
        }

        #endregion

        #region Data Integrity Tests

        [TestMethod]
        public async Task UpdateAsync_UserWithNullLastLoginAt_HandlesCorrectly()
        {
            // Arrange
            User originalUser = await CreateTestUser();
            User updatedUser = new User(
                originalUser.Id,
                originalUser.Email,
                originalUser.PasswordHash,
                originalUser.FirstName,
                originalUser.LastName,
                originalUser.IsActive,
                null, // Set LastLoginAt to null
                originalUser.CreatedAt,
                originalUser.UpdatedAt,
                originalUser.Roles);

            // Act
            User result = await _userRepository.UpdateAsync(updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.LastLoginAt);
        }

        [TestMethod]
        public async Task UpdateAsync_UserWithFutureLastLoginAt_HandlesCorrectly()
        {
            // Arrange
            User originalUser = await CreateTestUser();
            DateTime futureDate = DateTime.UtcNow.AddDays(1);
            User updatedUser = new User(
                originalUser.Id,
                originalUser.Email,
                originalUser.PasswordHash,
                originalUser.FirstName,
                originalUser.LastName,
                originalUser.IsActive,
                futureDate,
                originalUser.CreatedAt,
                originalUser.UpdatedAt,
                originalUser.Roles);

            // Act
            User result = await _userRepository.UpdateAsync(updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(futureDate, result.LastLoginAt);
        }

        [TestMethod]
        public async Task GetAllAsync_UsersWithMixedRoleScenarios_ReturnsCorrectData()
        {
            // Arrange
            User userWithRoles = await CreateTestUser("user1@example.com", "User", "One", new List<string> { "user", "admin" });
            User userWithoutRoles = await CreateTestUser("user2@example.com", "User", "Two", new List<string>());
            User userWithSingleRole = await CreateTestUser("user3@example.com", "User", "Three", new List<string> { "moderator" });

            // Act
            List<User> allUsers = await _userRepository.GetAllAsync();

            // Assert
            Assert.IsNotNull(allUsers);
            Assert.IsTrue(allUsers.Count >= 3);

            User? foundUserWithRoles = allUsers.FirstOrDefault(u => u.Id == userWithRoles.Id);
            User? foundUserWithoutRoles = allUsers.FirstOrDefault(u => u.Id == userWithoutRoles.Id);
            User? foundUserWithSingleRole = allUsers.FirstOrDefault(u => u.Id == userWithSingleRole.Id);

            Assert.IsNotNull(foundUserWithRoles);
            Assert.AreEqual(2, foundUserWithRoles.Roles.Count);
            Assert.IsTrue(foundUserWithRoles.Roles.Contains("user"));
            Assert.IsTrue(foundUserWithRoles.Roles.Contains("admin"));

            Assert.IsNotNull(foundUserWithoutRoles);
            Assert.AreEqual(0, foundUserWithoutRoles.Roles.Count);

            Assert.IsNotNull(foundUserWithSingleRole);
            Assert.AreEqual(1, foundUserWithSingleRole.Roles.Count);
            Assert.IsTrue(foundUserWithSingleRole.Roles.Contains("moderator"));
        }

        [TestMethod]
        public async Task CreateAsync_UserWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            string email = "test+special@example.com";
            string passwordHash = "hashed_password_123";
            string firstName = "José";
            string lastName = "O'Connor-Smith";
            List<string> roles = new List<string> { "user", "admin" };

            // Act
            User createdUser = await _userRepository.CreateAsync(email, passwordHash, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(email, createdUser.Email);
            Assert.AreEqual(firstName, createdUser.FirstName);
            Assert.AreEqual(lastName, createdUser.LastName);
            Assert.AreEqual(2, createdUser.Roles.Count);
        }

        [TestMethod]
        public async Task UpdateAsync_UserWithMaximumLengthFields_HandlesCorrectly()
        {
            // Arrange
            User originalUser = await CreateTestUser();
            string longEmail = "very.long.email.address.that.might.be.close.to.maximum.length@very.long.domain.name.com";
            string longFirstName = new string('a', 50);
            string longLastName = new string('b', 50);
            string longPasswordHash = new string('c', 255);

            User updatedUser = new User(
                originalUser.Id,
                longEmail,
                longPasswordHash,
                longFirstName,
                longLastName,
                originalUser.IsActive,
                originalUser.LastLoginAt,
                originalUser.CreatedAt,
                originalUser.UpdatedAt,
                originalUser.Roles);

            // Act
            User result = await _userRepository.UpdateAsync(updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(longEmail, result.Email);
            Assert.AreEqual(longFirstName, result.FirstName);
            Assert.AreEqual(longLastName, result.LastName);
            Assert.AreEqual(longPasswordHash, result.PasswordHash);
        }

        #endregion
    }
}

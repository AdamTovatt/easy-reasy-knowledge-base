using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

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

        [ClassCleanup]
        public static void ClassCleanup()
        {
            CleanupDatabaseTestEnvironment();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            SetupTestDatabase();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanupTestData();
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
            Assert.AreEqual(lastLoginAt, updatedUser.LastLoginAt);
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
    }
}

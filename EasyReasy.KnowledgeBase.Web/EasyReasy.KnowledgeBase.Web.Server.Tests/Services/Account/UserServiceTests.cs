using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Services.Account;
using EasyReasy.KnowledgeBase.Web.Server.Tests.Mocks;
using EasyReasy.Auth;
using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Services.Account
{
    /// <summary>
    /// Unit tests for the UserService using mock dependencies.
    /// </summary>
    [TestClass]
    public class UserServiceTests : GeneralTestBase
    {
        private MockUserRepository _mockUserRepository = null!;
        private IPasswordHasher _passwordHasher = null!;
        private UserService _userService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockUserRepository = new MockUserRepository();
            _passwordHasher = new SecurePasswordHasher();
            _userService = new UserService(_mockUserRepository, _passwordHasher);
            LoadTestEnvironmentVariables();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _mockUserRepository.Clear();
        }

        #region CreateUserAsync Tests

        [TestMethod]
        public async Task CreateUserAsync_ValidUser_CreatesUserSuccessfully()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user", "admin" };

            // Act
            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreNotEqual(Guid.Empty, createdUser.Id);
            Assert.AreEqual(email, createdUser.Email);
            Assert.AreEqual(firstName, createdUser.FirstName);
            Assert.AreEqual(lastName, createdUser.LastName);
            Assert.IsTrue(createdUser.IsActive);
            Assert.IsNull(createdUser.LastLoginAt);
            Assert.AreEqual(2, createdUser.Roles.Count);
            Assert.IsTrue(createdUser.Roles.Contains("user"));
            Assert.IsTrue(createdUser.Roles.Contains("admin"));
            Assert.IsTrue(createdUser.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(createdUser.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));

            // Verify password was hashed
            Assert.IsTrue(_passwordHasher.ValidatePassword(password, createdUser.PasswordHash, email));
        }

        [TestMethod]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            string email = "duplicate@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Create first user
            await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act & Assert - Try to create second user with same email
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _userService.CreateUserAsync(email, "different_password", "Jane", "Smith", roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            string email = "";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email, password, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_NullEmail_ThrowsArgumentException()
        {
            // Arrange
            string? email = null;
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email!, password, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_EmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string password = "";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email, password, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_NullPassword_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string? password = null;
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email, password!, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_EmptyFirstName_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email, password, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_EmptyLastName_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "";
            List<string> roles = new List<string> { "user" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _userService.CreateUserAsync(email, password, firstName, lastName, roles));
        }

        [TestMethod]
        public async Task CreateUserAsync_UserWithNoRoles_CreatesUserSuccessfully()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string>();

            // Act
            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(0, createdUser.Roles.Count);
        }

        #endregion

        #region ValidateCredentialsAsync Tests

        [TestMethod]
        public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.IsNotNull(validatedUser);
            Assert.AreEqual(createdUser.Id, validatedUser.Id);
            Assert.AreEqual(createdUser.Email, validatedUser.Email);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_InvalidEmail_ReturnsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync("wrong@example.com", password);

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, "wrongpassword");

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_InactiveUser_ReturnsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Deactivate the user by updating it in the mock repository
            User inactiveUser = new User(
                createdUser.Id,
                createdUser.Email,
                createdUser.PasswordHash,
                createdUser.FirstName,
                createdUser.LastName,
                false, // isActive = false
                createdUser.LastLoginAt,
                createdUser.CreatedAt,
                createdUser.UpdatedAt,
                createdUser.Roles);

            _mockUserRepository.AddUser(inactiveUser);

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_EmptyEmail_ReturnsNull()
        {
            // Arrange
            string email = "";
            string password = "password123";

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_EmptyPassword_ReturnsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "";

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_NullEmail_ReturnsNull()
        {
            // Arrange
            string? email = null;
            string password = "password123";

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email!, password);

            // Assert
            Assert.IsNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_NullPassword_ReturnsNull()
        {
            // Arrange
            string email = "test@example.com";
            string? password = null;

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password!);

            // Assert
            Assert.IsNull(validatedUser);
        }

        #endregion

        #region UpdateLastLoginAsync Tests

        [TestMethod]
        public async Task UpdateLastLoginAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            bool result = await _userService.UpdateLastLoginAsync(createdUser.Id);

            // Assert
            Assert.IsTrue(result);

            // Verify the update
            User? updatedUser = await _userService.GetUserByIdAsync(createdUser.Id);
            Assert.IsNotNull(updatedUser);
            Assert.IsNotNull(updatedUser.LastLoginAt);
            Assert.IsTrue(updatedUser.LastLoginAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [TestMethod]
        public async Task UpdateLastLoginAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _userService.UpdateLastLoginAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region UserExistsAsync Tests

        [TestMethod]
        public async Task UserExistsAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            bool exists = await _userService.UserExistsAsync(email);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task UserExistsAsync_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            string nonExistentEmail = "nonexistent@example.com";

            // Act
            bool exists = await _userService.UserExistsAsync(nonExistentEmail);

            // Assert
            Assert.IsFalse(exists);
        }

        #endregion

        #region GetUserByEmailAsync Tests

        [TestMethod]
        public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? retrievedUser = await _userService.GetUserByEmailAsync(email);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(createdUser.Id, retrievedUser.Id);
            Assert.AreEqual(createdUser.Email, retrievedUser.Email);
            Assert.AreEqual(createdUser.FirstName, retrievedUser.FirstName);
            Assert.AreEqual(createdUser.LastName, retrievedUser.LastName);
        }

        [TestMethod]
        public async Task GetUserByEmailAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange
            string nonExistentEmail = "nonexistent@example.com";

            // Act
            User? retrievedUser = await _userService.GetUserByEmailAsync(nonExistentEmail);

            // Assert
            Assert.IsNull(retrievedUser);
        }

        #endregion

        #region GetUserByIdAsync Tests

        [TestMethod]
        public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? retrievedUser = await _userService.GetUserByIdAsync(createdUser.Id);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(createdUser.Id, retrievedUser.Id);
            Assert.AreEqual(createdUser.Email, retrievedUser.Email);
            Assert.AreEqual(createdUser.FirstName, retrievedUser.FirstName);
            Assert.AreEqual(createdUser.LastName, retrievedUser.LastName);
        }

        [TestMethod]
        public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            User? retrievedUser = await _userService.GetUserByIdAsync(nonExistentId);

            // Assert
            Assert.IsNull(retrievedUser);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [TestMethod]
        public async Task CreateUserAsync_UserWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            string email = "test+special@example.com";
            string password = "password123";
            string firstName = "José";
            string lastName = "O'Connor-Smith";
            List<string> roles = new List<string> { "user", "admin" };

            // Act
            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(email, createdUser.Email);
            Assert.AreEqual(firstName, createdUser.FirstName);
            Assert.AreEqual(lastName, createdUser.LastName);
            Assert.AreEqual(2, createdUser.Roles.Count);

            // Verify password validation still works
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);
            Assert.IsNotNull(validatedUser);
        }

        [TestMethod]
        public async Task ValidateCredentialsAsync_PasswordWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            string email = "test@example.com";
            string password = "p@ssw0rd!@#$%^&*()";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user" };

            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Act
            User? validatedUser = await _userService.ValidateCredentialsAsync(email, password);

            // Assert
            Assert.IsNotNull(validatedUser);
            Assert.AreEqual(createdUser.Id, validatedUser.Id);
        }

        [TestMethod]
        public async Task CreateUserAsync_UserWithDuplicateRoles_HandlesCorrectly()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            string firstName = "John";
            string lastName = "Doe";
            List<string> roles = new List<string> { "user", "user", "admin" }; // Duplicate "user"

            // Act
            User createdUser = await _userService.CreateUserAsync(email, password, firstName, lastName, roles);

            // Assert
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(2, createdUser.Roles.Count); // Should deduplicate
            Assert.IsTrue(createdUser.Roles.Contains("user"));
            Assert.IsTrue(createdUser.Roles.Contains("admin"));
        }

        #endregion

        #region Password Hash Generation for Manual Database Insertion

        /// <summary>
        /// Environment variable configuration for password hash generation tests.
        /// </summary>
        [EnvironmentVariableNameContainer]
        public static class PasswordHashTestVariables
        {
            /// <summary>
            /// The password to hash for manual database insertion.
            /// </summary>
            [EnvironmentVariableName(minLength: 1)]
            public static readonly VariableName TestPassword = new VariableName("TEST_PASSWORD_TO_HASH");

            /// <summary>
            /// The email to use as salt for password hashing.
            /// </summary>
            [EnvironmentVariableName(minLength: 1)]
            public static readonly VariableName TestEmail = new VariableName("TEST_EMAIL_FOR_HASHING");
        }

        [TestMethod]
        public void GeneratePasswordHash_ForManualDatabaseInsertion_OutputsHashToConsole()
        {
            // This test reads environment variables to generate a password hash
            // that can be manually inserted into the database for testing purposes.

            try
            {
                // Read environment variables
                string password = PasswordHashTestVariables.TestPassword.GetValue();
                string email = PasswordHashTestVariables.TestEmail.GetValue();

                // Generate the password hash
                string passwordHash = _passwordHasher.HashPassword(password, email);

                // Output the results to console for manual database insertion
                Console.WriteLine("=== PASSWORD HASH GENERATION FOR MANUAL DATABASE INSERTION ===");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine($"Password: {password}");
                Console.WriteLine($"Generated Hash: {passwordHash}");
                Console.WriteLine("=== END PASSWORD HASH GENERATION ===");

                // Verify the hash works by validating it
                bool isValid = _passwordHasher.ValidatePassword(password, passwordHash, email);
                Assert.IsTrue(isValid, "Generated password hash should be valid");

                Console.WriteLine($"Hash validation successful: {isValid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== PASSWORD HASH GENERATION SKIPPED ===");
                Console.WriteLine($"Missing environment variable: {ex.Message}");
                Console.WriteLine("To generate a password hash, set these environment variables:");
                Console.WriteLine("  TEST_PASSWORD_TO_HASH=your_password_here");
                Console.WriteLine("  TEST_EMAIL_FOR_HASHING=user@example.com");
                Console.WriteLine("=== END PASSWORD HASH GENERATION ===");

                // Skip the test if environment variables are not set
                Assert.Inconclusive("Environment variables not set for password hash generation");
            }
        }

        #endregion
    }
}

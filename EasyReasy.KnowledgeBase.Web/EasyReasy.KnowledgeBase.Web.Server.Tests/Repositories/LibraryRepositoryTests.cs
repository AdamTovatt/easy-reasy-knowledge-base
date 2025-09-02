using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Repositories
{
    /// <summary>
    /// Integration tests for the LibraryRepository using a real PostgreSQL database.
    /// </summary>
    [TestClass]
    public class LibraryRepositoryTests : DatabaseTestBase
    {
        private static ILibraryRepository _libraryRepository = null!;
        private static IUserRepository _userRepository = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseTestEnvironment();
            _libraryRepository = new LibraryRepository(_connectionFactory);
            _userRepository = new UserRepository(_connectionFactory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test data after each test (order matters due to foreign keys)
            string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            TestDatabaseMigrator.CleanupTable(connectionString, "library", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);
        }

        #region CreateAsync Tests

        [TestMethod]
        public async Task CreateAsync_WithValidData_CreatesLibrarySuccessfully()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string name = "Test Library";
            string description = "A test library for unit testing";
            bool isPublic = false;

            // Act
            Library createdLibrary = await _libraryRepository.CreateAsync(name, description, ownerId, isPublic);

            // Assert
            Assert.IsNotNull(createdLibrary);
            Assert.AreNotEqual(Guid.Empty, createdLibrary.Id);
            Assert.AreEqual(name, createdLibrary.Name);
            Assert.AreEqual(description, createdLibrary.Description);
            Assert.AreEqual(ownerId, createdLibrary.OwnerId);
            Assert.AreEqual(isPublic, createdLibrary.IsPublic);
            Assert.IsTrue(createdLibrary.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(createdLibrary.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [TestMethod]
        public async Task CreateAsync_WithNullDescription_CreatesLibrarySuccessfully()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string name = "Library with Null Description";
            string? description = null;

            // Act
            Library createdLibrary = await _libraryRepository.CreateAsync(name, description, ownerId);

            // Assert
            Assert.IsNotNull(createdLibrary);
            Assert.AreEqual(name, createdLibrary.Name);
            Assert.IsNull(createdLibrary.Description);
            Assert.IsFalse(createdLibrary.IsPublic); // Default value
        }

        [TestMethod]
        public async Task CreateAsync_WithPublicLibrary_CreatesLibrarySuccessfully()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string name = "Public Library";
            string description = "A public library for everyone";
            bool isPublic = true;

            // Act
            Library createdLibrary = await _libraryRepository.CreateAsync(name, description, ownerId, isPublic);

            // Assert
            Assert.IsNotNull(createdLibrary);
            Assert.AreEqual(name, createdLibrary.Name);
            Assert.AreEqual(description, createdLibrary.Description);
            Assert.IsTrue(createdLibrary.IsPublic);
        }

        [TestMethod]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryRepository.CreateAsync(null!, "Description", ownerId));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryRepository.CreateAsync("", "Description", ownerId));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithWhitespaceOnlyName_ThrowsArgumentException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryRepository.CreateAsync("   ", "Description", ownerId));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithNonExistentOwner_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid nonExistentOwnerId = Guid.NewGuid();

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryRepository.CreateAsync("Test Library", "Description", nonExistentOwnerId));

            Assert.IsTrue(exception.Message.Contains("does not exist"));
        }

        [TestMethod]
        public async Task CreateAsync_WithDuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string duplicateName = "Duplicate Library Name";

            await _libraryRepository.CreateAsync(duplicateName, "First description", ownerId);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryRepository.CreateAsync(duplicateName, "Second description", ownerId));

            Assert.IsTrue(exception.Message.Contains("already exists"));
        }

        #endregion

        #region GetByIdAsync Tests

        [TestMethod]
        public async Task GetByIdAsync_WithExistingLibrary_ReturnsLibrary()
        {
            // Arrange
            Library createdLibrary = await CreateTestLibrary();

            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByIdAsync(createdLibrary.Id);

            // Assert
            Assert.IsNotNull(retrievedLibrary);
            Assert.AreEqual(createdLibrary.Id, retrievedLibrary.Id);
            Assert.AreEqual(createdLibrary.Name, retrievedLibrary.Name);
            Assert.AreEqual(createdLibrary.Description, retrievedLibrary.Description);
            Assert.AreEqual(createdLibrary.OwnerId, retrievedLibrary.OwnerId);
            Assert.AreEqual(createdLibrary.IsPublic, retrievedLibrary.IsPublic);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithNonExistentLibrary_ReturnsNull()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.IsNull(retrievedLibrary);
        }

        #endregion

        #region GetByNameAsync Tests

        [TestMethod]
        public async Task GetByNameAsync_WithExistingLibrary_ReturnsLibrary()
        {
            // Arrange
            Library createdLibrary = await CreateTestLibrary("Unique Library Name");

            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByNameAsync(createdLibrary.Name);

            // Assert
            Assert.IsNotNull(retrievedLibrary);
            Assert.AreEqual(createdLibrary.Id, retrievedLibrary.Id);
            Assert.AreEqual(createdLibrary.Name, retrievedLibrary.Name);
        }

        [TestMethod]
        public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
        {
            // Arrange
            string nonExistentName = "Non-Existent Library";

            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByNameAsync(nonExistentName);

            // Assert
            Assert.IsNull(retrievedLibrary);
        }

        [TestMethod]
        public async Task GetByNameAsync_WithNullName_ReturnsNull()
        {
            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByNameAsync(null!);

            // Assert
            Assert.IsNull(retrievedLibrary);
        }

        [TestMethod]
        public async Task GetByNameAsync_WithEmptyName_ReturnsNull()
        {
            // Act
            Library? retrievedLibrary = await _libraryRepository.GetByNameAsync("");

            // Assert
            Assert.IsNull(retrievedLibrary);
        }

        #endregion

        #region UpdateAsync Tests

        [TestMethod]
        public async Task UpdateAsync_WithExistingLibrary_UpdatesLibrarySuccessfully()
        {
            // Arrange
            Library originalLibrary = await CreateTestLibrary();
            Library updatedLibrary = new Library(
                originalLibrary.Id,
                "Updated Library Name",
                "Updated description",
                originalLibrary.OwnerId,
                true, // Changed from false to true
                originalLibrary.CreatedAt,
                originalLibrary.UpdatedAt
            );

            // Act
            Library result = await _libraryRepository.UpdateAsync(updatedLibrary);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updatedLibrary.Id, result.Id);
            Assert.AreEqual(updatedLibrary.Name, result.Name);
            Assert.AreEqual(updatedLibrary.Description, result.Description);
            Assert.AreEqual(updatedLibrary.OwnerId, result.OwnerId);
            Assert.AreEqual(updatedLibrary.IsPublic, result.IsPublic);
            Assert.AreEqual(originalLibrary.CreatedAt, result.CreatedAt);
            Assert.IsTrue(result.UpdatedAt >= originalLibrary.UpdatedAt);
        }

        [TestMethod]
        public async Task UpdateAsync_WithNonExistentLibrary_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Library nonExistentLibrary = new Library(
                Guid.NewGuid(),
                "Non-Existent Library",
                "Description",
                ownerId,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow
            );

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryRepository.UpdateAsync(nonExistentLibrary));

            Assert.IsTrue(exception.Message.Contains("not found"));
        }

        [TestMethod]
        public async Task UpdateAsync_WithDuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Library library1 = await CreateTestLibrary("Library One", ownerId);
            Library library2 = await CreateTestLibrary("Library Two", ownerId);

            // Update library2 to have the same name as library1
            Library updatedLibrary2 = new Library(
                library2.Id,
                library1.Name, // Duplicate name
                library2.Description,
                library2.OwnerId,
                library2.IsPublic,
                library2.CreatedAt,
                library2.UpdatedAt
            );

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryRepository.UpdateAsync(updatedLibrary2));

            Assert.IsTrue(exception.Message.Contains("already exists"));
        }

        #endregion

        #region DeleteAsync Tests

        [TestMethod]
        public async Task DeleteAsync_WithExistingLibrary_DeletesLibrarySuccessfully()
        {
            // Arrange
            Library createdLibrary = await CreateTestLibrary();

            // Act
            bool result = await _libraryRepository.DeleteAsync(createdLibrary.Id);

            // Assert
            Assert.IsTrue(result);

            // Verify library is deleted
            Library? deletedLibrary = await _libraryRepository.GetByIdAsync(createdLibrary.Id);
            Assert.IsNull(deletedLibrary);
        }

        [TestMethod]
        public async Task DeleteAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _libraryRepository.DeleteAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region GetByOwnerIdAsync Tests

        [TestMethod]
        public async Task GetByOwnerIdAsync_WithLibraries_ReturnsAllOwnerLibraries()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Guid otherOwnerId = await CreateTestUser("other@example.com");

            Library library1 = await CreateTestLibrary("Owner Library 1", ownerId);
            Library library2 = await CreateTestLibrary("Owner Library 2", ownerId);
            await CreateTestLibrary("Other Owner Library", otherOwnerId); // Should not be returned

            // Act
            List<Library> ownerLibraries = await _libraryRepository.GetByOwnerIdAsync(ownerId);

            // Assert
            Assert.AreEqual(2, ownerLibraries.Count);
            Assert.IsTrue(ownerLibraries.Any(l => l.Id == library1.Id));
            Assert.IsTrue(ownerLibraries.Any(l => l.Id == library2.Id));
            Assert.IsTrue(ownerLibraries.All(l => l.OwnerId == ownerId));
            // Verify ordering (newest first)
            Assert.IsTrue(ownerLibraries[0].CreatedAt >= ownerLibraries[1].CreatedAt);
        }

        [TestMethod]
        public async Task GetByOwnerIdAsync_WithNoLibraries_ReturnsEmptyList()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();

            // Act
            List<Library> ownerLibraries = await _libraryRepository.GetByOwnerIdAsync(ownerId);

            // Assert
            Assert.IsNotNull(ownerLibraries);
            Assert.AreEqual(0, ownerLibraries.Count);
        }

        #endregion

        #region GetPublicLibrariesAsync Tests

        [TestMethod]
        public async Task GetPublicLibrariesAsync_WithPublicLibraries_ReturnsOnlyPublicLibraries()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Library publicLibrary1 = await CreateTestLibrary("Public Library 1", ownerId, isPublic: true);
            Library publicLibrary2 = await CreateTestLibrary("Public Library 2", ownerId, isPublic: true);
            await CreateTestLibrary("Private Library", ownerId, isPublic: false); // Should not be returned

            // Act
            List<Library> publicLibraries = await _libraryRepository.GetPublicLibrariesAsync();

            // Assert
            Assert.AreEqual(2, publicLibraries.Count);
            Assert.IsTrue(publicLibraries.Any(l => l.Id == publicLibrary1.Id));
            Assert.IsTrue(publicLibraries.Any(l => l.Id == publicLibrary2.Id));
            Assert.IsTrue(publicLibraries.All(l => l.IsPublic));
            // Verify ordering (newest first)
            Assert.IsTrue(publicLibraries[0].CreatedAt >= publicLibraries[1].CreatedAt);
        }

        [TestMethod]
        public async Task GetPublicLibrariesAsync_WithNoPublicLibraries_ReturnsEmptyList()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            await CreateTestLibrary("Private Library", ownerId, isPublic: false);

            // Act
            List<Library> publicLibraries = await _libraryRepository.GetPublicLibrariesAsync();

            // Assert
            Assert.IsNotNull(publicLibraries);
            Assert.AreEqual(0, publicLibraries.Count);
        }

        #endregion

        #region ExistsAsync Tests

        [TestMethod]
        public async Task ExistsAsync_WithExistingLibrary_ReturnsTrue()
        {
            // Arrange
            Library createdLibrary = await CreateTestLibrary();

            // Act
            bool exists = await _libraryRepository.ExistsAsync(createdLibrary.Id);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ExistsAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool exists = await _libraryRepository.ExistsAsync(nonExistentId);

            // Assert
            Assert.IsFalse(exists);
        }

        #endregion

        #region IsOwnerAsync Tests

        [TestMethod]
        public async Task IsOwnerAsync_WithCorrectOwner_ReturnsTrue()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Library createdLibrary = await CreateTestLibrary("Owner Test Library", ownerId);

            // Act
            bool isOwner = await _libraryRepository.IsOwnerAsync(createdLibrary.Id, ownerId);

            // Assert
            Assert.IsTrue(isOwner);
        }

        [TestMethod]
        public async Task IsOwnerAsync_WithWrongOwner_ReturnsFalse()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Guid otherUserId = await CreateTestUser("other@example.com");
            Library createdLibrary = await CreateTestLibrary("Owner Test Library", ownerId);

            // Act
            bool isOwner = await _libraryRepository.IsOwnerAsync(createdLibrary.Id, otherUserId);

            // Assert
            Assert.IsFalse(isOwner);
        }

        [TestMethod]
        public async Task IsOwnerAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Guid nonExistentLibraryId = Guid.NewGuid();

            // Act
            bool isOwner = await _libraryRepository.IsOwnerAsync(nonExistentLibraryId, ownerId);

            // Assert
            Assert.IsFalse(isOwner);
        }

        [TestMethod]
        public async Task IsOwnerAsync_WithNonExistentUser_ReturnsFalse()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Guid nonExistentUserId = Guid.NewGuid();
            Library createdLibrary = await CreateTestLibrary("Owner Test Library", ownerId);

            // Act
            bool isOwner = await _libraryRepository.IsOwnerAsync(createdLibrary.Id, nonExistentUserId);

            // Assert
            Assert.IsFalse(isOwner);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [TestMethod]
        public async Task CreateAsync_WithUnicodeName_HandlesCorrectly()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string unicodeName = "测试图书馆 📚 Émojis & Special-Chars_123";
            string unicodeDescription = "This is a library with unicode: 测试 🚀 émojis and special chars";

            // Act
            Library createdLibrary = await _libraryRepository.CreateAsync(unicodeName, unicodeDescription, ownerId);

            // Assert
            Assert.IsNotNull(createdLibrary);
            Assert.AreEqual(unicodeName, createdLibrary.Name);
            Assert.AreEqual(unicodeDescription, createdLibrary.Description);
        }

        [TestMethod]
        public async Task CreateAsync_WithVeryLongName_HandlesCorrectly()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            string longName = new string('A', 200); // Long but within reasonable limits
            string longDescription = new string('B', 1000);

            // Act
            Library createdLibrary = await _libraryRepository.CreateAsync(longName, longDescription, ownerId);

            // Assert
            Assert.IsNotNull(createdLibrary);
            Assert.AreEqual(longName, createdLibrary.Name);
            Assert.AreEqual(longDescription, createdLibrary.Description);
        }

        [TestMethod]
        public async Task UpdateAsync_WithNullDescription_UpdatesCorrectly()
        {
            // Arrange
            Library originalLibrary = await CreateTestLibrary();
            Library updatedLibrary = new Library(
                originalLibrary.Id,
                originalLibrary.Name,
                null, // Set description to null
                originalLibrary.OwnerId,
                originalLibrary.IsPublic,
                originalLibrary.CreatedAt,
                originalLibrary.UpdatedAt
            );

            // Act
            Library result = await _libraryRepository.UpdateAsync(updatedLibrary);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Description);
        }

        [TestMethod]
        public async Task MultipleOperations_WorkCorrectly()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();

            // Act & Assert - Create
            Library createdLibrary = await _libraryRepository.CreateAsync("Integration Test Library", "Original description", ownerId);
            Assert.IsNotNull(createdLibrary);

            // Act & Assert - Get by ID
            Library? retrievedById = await _libraryRepository.GetByIdAsync(createdLibrary.Id);
            Assert.IsNotNull(retrievedById);
            Assert.AreEqual(createdLibrary.Name, retrievedById.Name);

            // Act & Assert - Get by Name
            Library? retrievedByName = await _libraryRepository.GetByNameAsync(createdLibrary.Name);
            Assert.IsNotNull(retrievedByName);
            Assert.AreEqual(createdLibrary.Id, retrievedByName.Id);

            // Act & Assert - Update
            Library updatedLibrary = new Library(
                createdLibrary.Id, "Updated Integration Test Library", "Updated description",
                createdLibrary.OwnerId, true, createdLibrary.CreatedAt, createdLibrary.UpdatedAt);
            Library result = await _libraryRepository.UpdateAsync(updatedLibrary);
            Assert.AreEqual("Updated Integration Test Library", result.Name);
            Assert.IsTrue(result.IsPublic);

            // Act & Assert - Check ownership
            bool isOwner = await _libraryRepository.IsOwnerAsync(createdLibrary.Id, ownerId);
            Assert.IsTrue(isOwner);

            // Act & Assert - Check existence
            bool exists = await _libraryRepository.ExistsAsync(createdLibrary.Id);
            Assert.IsTrue(exists);

            // Act & Assert - Delete
            bool deleted = await _libraryRepository.DeleteAsync(createdLibrary.Id);
            Assert.IsTrue(deleted);

            // Act & Assert - Verify deletion
            bool stillExists = await _libraryRepository.ExistsAsync(createdLibrary.Id);
            Assert.IsFalse(stillExists);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test user and returns the ID.
        /// </summary>
        private async Task<Guid> CreateTestUser(string email = "test@example.com")
        {
            User user = await _userRepository.CreateAsync(
                email, "hashed_password", "John", "Doe", new List<string> { "user" });
            return user.Id;
        }

        /// <summary>
        /// Creates a test library with default values.
        /// </summary>
        private async Task<Library> CreateTestLibrary(string? name = null, Guid? ownerId = null, bool isPublic = false)
        {
            Guid finalOwnerId = ownerId ?? await CreateTestUser();
            string finalName = name ?? "Test Library";

            return await _libraryRepository.CreateAsync(
                finalName, "A test library for unit testing", finalOwnerId, isPublic);
        }

        #endregion
    }
}

using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Repositories
{
    /// <summary>
    /// Integration tests for the LibraryFileRepository using a real PostgreSQL database.
    /// </summary>
    [TestClass]
    public class LibraryFileRepositoryTests : DatabaseTestBase
    {
        private static ILibraryFileRepository _libraryFileRepository = null!;
        private static ILibraryRepository _libraryRepository = null!;
        private static IUserRepository _userRepository = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseTestEnvironment();
            _libraryFileRepository = new LibraryFileRepository(_connectionFactory);
            _libraryRepository = new LibraryRepository(_connectionFactory);
            _userRepository = new UserRepository(_connectionFactory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test data after each test (order matters due to foreign keys)
            string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            TestDatabaseMigrator.CleanupTable(connectionString, "library_file", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "library", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);
        }

        #region CreateAsync Tests

        [TestMethod]
        public async Task CreateAsync_WithValidData_CreatesFileSuccessfully()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            string originalFileName = "test-document.pdf";
            string contentType = "application/pdf";
            long sizeInBytes = 2048;
            string relativePath = "lib_test/test-document.pdf";
            byte[] hash = CreateTestHash();

            // Act
            LibraryFile createdFile = await _libraryFileRepository.CreateAsync(
                libraryId, originalFileName, contentType, sizeInBytes, relativePath, hash, userId);

            // Assert
            Assert.IsNotNull(createdFile);
            Assert.AreNotEqual(Guid.Empty, createdFile.Id);
            Assert.AreEqual(libraryId, createdFile.LibraryId);
            Assert.AreEqual(originalFileName, createdFile.OriginalFileName);
            Assert.AreEqual(contentType, createdFile.ContentType);
            Assert.AreEqual(sizeInBytes, createdFile.SizeInBytes);
            Assert.AreEqual(relativePath, createdFile.RelativePath);
            CollectionAssert.AreEqual(hash, createdFile.Hash);
            Assert.AreEqual(userId, createdFile.UploadedByUserId);
            Assert.IsTrue(createdFile.UploadedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(createdFile.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.IsTrue(createdFile.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [TestMethod]
        public async Task CreateAsync_WithNullOriginalFileName_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, null!, "text/plain", 1024, "lib_test/test.txt", CreateTestHash(), userId));
            
            Assert.AreEqual("originalKnowledgeFileName", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithEmptyOriginalFileName_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "", "text/plain", 1024, "lib_test/test.txt", CreateTestHash(), userId));
            
            Assert.AreEqual("originalKnowledgeFileName", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithNullContentType_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "test.txt", null!, 1024, "lib_test/test.txt", CreateTestHash(), userId));
            
            Assert.AreEqual("contentType", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithNullRelativePath_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "test.txt", "text/plain", 1024, null!, CreateTestHash(), userId));
            
            Assert.AreEqual("relativePath", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithNegativeSize_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "test.txt", "text/plain", -1, "lib_test/test.txt", CreateTestHash(), userId));
            
            Assert.AreEqual("sizeInBytes", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithNullHash_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "test.txt", "text/plain", 1024, "lib_test/test.txt", null!, userId));
            
            Assert.AreEqual("hash", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithEmptyHash_ThrowsArgumentException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "test.txt", "text/plain", 1024, "lib_test/test.txt", Array.Empty<byte>(), userId));
            
            Assert.AreEqual("hash", exception.ParamName);
        }

        [TestMethod]
        public async Task CreateAsync_WithDuplicateRelativePath_ThrowsInvalidOperationException()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            string duplicatePath = "lib_test/duplicate.txt";
            
            await _libraryFileRepository.CreateAsync(
                libraryId, "first.txt", "text/plain", 1024, duplicatePath, CreateTestHash(), userId);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryFileRepository.CreateAsync(
                    libraryId, "second.txt", "text/plain", 2048, duplicatePath, CreateTestHash(), userId));

            Assert.IsTrue(exception.Message.Contains("already exists"));
        }

        [TestMethod]
        public async Task CreateAsync_WithNonExistentLibrary_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid userId = await CreateTestUser();
            Guid nonExistentLibraryId = Guid.NewGuid();

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _libraryFileRepository.CreateAsync(
                    nonExistentLibraryId, "test.txt", "text/plain", 1024, "lib_test/test.txt", CreateTestHash(), userId));

            Assert.IsTrue(exception.Message.Contains("does not exist"));
        }

        #endregion

        #region GetByIdAsync Tests

        [TestMethod]
        public async Task GetByIdAsync_WithExistingFile_ReturnsFile()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();

            // Act
            LibraryFile? retrievedFile = await _libraryFileRepository.GetByIdAsync(createdFile.Id);

            // Assert
            Assert.IsNotNull(retrievedFile);
            Assert.AreEqual(createdFile.Id, retrievedFile.Id);
            Assert.AreEqual(createdFile.LibraryId, retrievedFile.LibraryId);
            Assert.AreEqual(createdFile.OriginalFileName, retrievedFile.OriginalFileName);
            Assert.AreEqual(createdFile.ContentType, retrievedFile.ContentType);
            Assert.AreEqual(createdFile.SizeInBytes, retrievedFile.SizeInBytes);
            Assert.AreEqual(createdFile.RelativePath, retrievedFile.RelativePath);
            CollectionAssert.AreEqual(createdFile.Hash, retrievedFile.Hash);
            Assert.AreEqual(createdFile.UploadedByUserId, retrievedFile.UploadedByUserId);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            LibraryFile? retrievedFile = await _libraryFileRepository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.IsNull(retrievedFile);
        }

        #endregion

        #region GetByIdInKnowledgeBaseAsync Tests

        [TestMethod]
        public async Task GetByIdInKnowledgeBaseAsync_WithExistingFile_ReturnsFile()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();

            // Act
            LibraryFile? retrievedFile = await _libraryFileRepository.GetByIdInKnowledgeBaseAsync(
                createdFile.LibraryId, createdFile.Id);

            // Assert
            Assert.IsNotNull(retrievedFile);
            Assert.AreEqual(createdFile.Id, retrievedFile.Id);
            Assert.AreEqual(createdFile.LibraryId, retrievedFile.LibraryId);
        }

        [TestMethod]
        public async Task GetByIdInKnowledgeBaseAsync_WithWrongLibrary_ReturnsNull()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();
            Guid differentLibraryId = Guid.NewGuid();

            // Act
            LibraryFile? retrievedFile = await _libraryFileRepository.GetByIdInKnowledgeBaseAsync(
                differentLibraryId, createdFile.Id);

            // Assert
            Assert.IsNull(retrievedFile);
        }

        [TestMethod]
        public async Task GetByIdInKnowledgeBaseAsync_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            (Guid libraryId, _) = await CreateTestLibraryAndUser();
            Guid nonExistentFileId = Guid.NewGuid();

            // Act
            LibraryFile? retrievedFile = await _libraryFileRepository.GetByIdInKnowledgeBaseAsync(
                libraryId, nonExistentFileId);

            // Assert
            Assert.IsNull(retrievedFile);
        }

        #endregion

        #region GetByKnowledgeBaseIdAsync Tests

        [TestMethod]
        public async Task GetByKnowledgeBaseIdAsync_WithFiles_ReturnsAllFiles()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            LibraryFile file1 = await CreateTestFile(libraryId, userId, "file1.txt");
            LibraryFile file2 = await CreateTestFile(libraryId, userId, "file2.txt");

            // Act
            List<LibraryFile> files = await _libraryFileRepository.GetByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(2, files.Count);
            Assert.IsTrue(files.Any(f => f.Id == file1.Id));
            Assert.IsTrue(files.Any(f => f.Id == file2.Id));
            // Verify ordering (newest first)
            Assert.IsTrue(files[0].UploadedAt >= files[1].UploadedAt);
        }

        [TestMethod]
        public async Task GetByKnowledgeBaseIdAsync_WithNoFiles_ReturnsEmptyList()
        {
            // Arrange
            (Guid libraryId, _) = await CreateTestLibraryAndUser();

            // Act
            List<LibraryFile> files = await _libraryFileRepository.GetByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.IsNotNull(files);
            Assert.AreEqual(0, files.Count);
        }

        [TestMethod]
        public async Task GetByKnowledgeBaseIdAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            await CreateTestFile(libraryId, userId, "file1.txt");
            await CreateTestFile(libraryId, userId, "file2.txt");
            await CreateTestFile(libraryId, userId, "file3.txt");

            // Act - Get second page with limit of 2
            List<LibraryFile> firstPage = await _libraryFileRepository.GetByKnowledgeBaseIdAsync(libraryId, 0, 2);
            List<LibraryFile> secondPage = await _libraryFileRepository.GetByKnowledgeBaseIdAsync(libraryId, 2, 2);

            // Assert
            Assert.AreEqual(2, firstPage.Count);
            Assert.AreEqual(1, secondPage.Count);
            // Ensure no overlap
            Assert.IsFalse(firstPage.Any(f => secondPage.Any(s => s.Id == f.Id)));
        }

        #endregion

        #region DeleteAsync Tests

        [TestMethod]
        public async Task DeleteAsync_WithExistingFile_DeletesFileSuccessfully()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();

            // Act
            bool result = await _libraryFileRepository.DeleteAsync(createdFile.Id);

            // Assert
            Assert.IsTrue(result);

            // Verify file is deleted
            LibraryFile? deletedFile = await _libraryFileRepository.GetByIdAsync(createdFile.Id);
            Assert.IsNull(deletedFile);
        }

        [TestMethod]
        public async Task DeleteAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _libraryFileRepository.DeleteAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region ExistsAsync Tests

        [TestMethod]
        public async Task ExistsAsync_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();

            // Act
            bool exists = await _libraryFileRepository.ExistsAsync(createdFile.Id);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ExistsAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool exists = await _libraryFileRepository.ExistsAsync(nonExistentId);

            // Assert
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public async Task ExistsInKnowledgeBaseAsync_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();

            // Act
            bool exists = await _libraryFileRepository.ExistsInKnowledgeBaseAsync(
                createdFile.LibraryId, createdFile.Id);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task ExistsInKnowledgeBaseAsync_WithWrongLibrary_ReturnsFalse()
        {
            // Arrange
            LibraryFile createdFile = await CreateTestFile();
            Guid differentLibraryId = Guid.NewGuid();

            // Act
            bool exists = await _libraryFileRepository.ExistsInKnowledgeBaseAsync(
                differentLibraryId, createdFile.Id);

            // Assert
            Assert.IsFalse(exists);
        }

        #endregion

        #region Statistics Tests

        [TestMethod]
        public async Task GetCountByKnowledgeBaseIdAsync_WithFiles_ReturnsCorrectCount()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            await CreateTestFile(libraryId, userId, "file1.txt");
            await CreateTestFile(libraryId, userId, "file2.txt");

            // Act
            long count = await _libraryFileRepository.GetCountByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetCountByKnowledgeBaseIdAsync_WithNoFiles_ReturnsZero()
        {
            // Arrange
            (Guid libraryId, _) = await CreateTestLibraryAndUser();

            // Act
            long count = await _libraryFileRepository.GetCountByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task GetTotalSizeByKnowledgeBaseIdAsync_WithFiles_ReturnsCorrectSize()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            await CreateTestFile(libraryId, userId, "file1.txt", sizeInBytes: 1024);
            await CreateTestFile(libraryId, userId, "file2.txt", sizeInBytes: 2048);

            // Act
            long totalSize = await _libraryFileRepository.GetTotalSizeByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(3072, totalSize);
        }

        [TestMethod]
        public async Task GetTotalSizeByKnowledgeBaseIdAsync_WithNoFiles_ReturnsZero()
        {
            // Arrange
            (Guid libraryId, _) = await CreateTestLibraryAndUser();

            // Act
            long totalSize = await _libraryFileRepository.GetTotalSizeByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(0, totalSize);
        }

        #endregion

        #region DeleteByKnowledgeBaseIdAsync Tests

        [TestMethod]
        public async Task DeleteByKnowledgeBaseIdAsync_WithFiles_DeletesAllFiles()
        {
            // Arrange
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            await CreateTestFile(libraryId, userId, "file1.txt");
            await CreateTestFile(libraryId, userId, "file2.txt");

            // Act
            int deletedCount = await _libraryFileRepository.DeleteByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(2, deletedCount);

            // Verify all files are deleted
            List<LibraryFile> remainingFiles = await _libraryFileRepository.GetByKnowledgeBaseIdAsync(libraryId);
            Assert.AreEqual(0, remainingFiles.Count);
        }

        [TestMethod]
        public async Task DeleteByKnowledgeBaseIdAsync_WithNoFiles_ReturnsZero()
        {
            // Arrange
            (Guid libraryId, _) = await CreateTestLibraryAndUser();

            // Act
            int deletedCount = await _libraryFileRepository.DeleteByKnowledgeBaseIdAsync(libraryId);

            // Assert
            Assert.AreEqual(0, deletedCount);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test library and user, returning their IDs.
        /// </summary>
        private async Task<(Guid LibraryId, Guid UserId)> CreateTestLibraryAndUser()
        {
            // Create user first
            User user = await _userRepository.CreateAsync(
                "test@example.com", "hashed_password", "John", "Doe", new List<string> { "user" });

            // Create library
            Library library = await _libraryRepository.CreateAsync(
                "Test Library", "A test library", user.Id, false);

            return (library.Id, user.Id);
        }

        /// <summary>
        /// Creates a test user and returns the ID.
        /// </summary>
        private async Task<Guid> CreateTestUser()
        {
            User user = await _userRepository.CreateAsync(
                "test@example.com", "hashed_password", "John", "Doe", new List<string> { "user" });
            return user.Id;
        }

        /// <summary>
        /// Creates a test file with default values.
        /// </summary>
        private async Task<LibraryFile> CreateTestFile()
        {
            (Guid libraryId, Guid userId) = await CreateTestLibraryAndUser();
            return await CreateTestFile(libraryId, userId);
        }

        /// <summary>
        /// Creates a test file with specified library and user.
        /// </summary>
        private async Task<LibraryFile> CreateTestFile(Guid libraryId, Guid userId, string fileName = "test.txt", long sizeInBytes = 1024)
        {
            return await _libraryFileRepository.CreateAsync(
                libraryId: libraryId,
                originalFileName: fileName,
                contentType: "text/plain",
                sizeInBytes: sizeInBytes,
                relativePath: $"lib_test/{fileName}",
                hash: CreateTestHash(),
                uploadedByUserId: userId);
        }

        /// <summary>
        /// Creates a test hash byte array.
        /// </summary>
        private static byte[] CreateTestHash()
        {
            return new byte[]
            {
                0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
                0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00
            };
        }

        #endregion
    }
}

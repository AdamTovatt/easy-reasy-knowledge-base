using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.Storage.Postgres;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    [TestClass]
    public sealed class PostgresFileStoreTests : PostgresTestBase<PostgresFileStoreTests>
    {
        [ClassInitialize]
        public static void BeforeAll(TestContext testContext)
        {
            InitializeTestEnvironment();
        }

        [ClassCleanup]
        public static void AfterAll()
        {
            CleanupTestEnvironment();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            SetupDatabaseForTest();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanupDatabaseAfterTest();
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenConnectionFactoryIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresFileStore(null!));
        }

        [TestMethod]
        public void Constructor_ShouldAccept_WhenConnectionFactoryIsValid()
        {
            // Act & Assert
            PostgresFileStore store = new PostgresFileStore(_connectionFactory);
            Assert.IsNotNull(store);
        }

        [TestMethod]
        public async Task AddAsync_ShouldAddFile_WhenValidFileProvided()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });

            // Act
            Guid result = await _fileStore.AddAsync(file);

            // Assert
            Assert.AreEqual(file.Id, result);
            KnowledgeFile? retrieved = await _fileStore.GetAsync(file.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(file.Id, retrieved.Id);
            Assert.AreEqual(file.Name, retrieved.Name);
            CollectionAssert.AreEqual(file.Hash, retrieved.Hash);
        }

        [TestMethod]
        public async Task AddAsync_ShouldThrow_WhenFileIsNull()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _fileStore.AddAsync(null!));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnFile_WhenFileExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            // Act
            KnowledgeFile? result = await _fileStore.GetAsync(file.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(file.Id, result.Id);
            Assert.AreEqual(file.Name, result.Name);
            CollectionAssert.AreEqual(file.Hash, result.Hash);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnNull_WhenFileDoesNotExist()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            KnowledgeFile? result = await _fileStore.GetAsync(nonExistentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ExistsAsync_ShouldReturnTrue_WhenFileExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            // Act
            bool result = await _fileStore.ExistsAsync(file.Id);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ExistsAsync_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _fileStore.ExistsAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnEmptyCollection_WhenNoFilesExist()
        {
            // Act
            IEnumerable<KnowledgeFile> result = await _fileStore.GetAllAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllFiles_WhenFilesExist()
        {
            // Arrange
            KnowledgeFile file1 = new KnowledgeFile(Guid.NewGuid(), "test1.txt", new byte[] { 1, 2, 3, 4 });
            KnowledgeFile file2 = new KnowledgeFile(Guid.NewGuid(), "test2.txt", new byte[] { 5, 6, 7, 8 });
            
            await _fileStore.AddAsync(file1);
            await _fileStore.AddAsync(file2);

            // Act
            IEnumerable<KnowledgeFile> result = await _fileStore.GetAllAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(f => f.Id == file1.Id));
            Assert.IsTrue(result.Any(f => f.Id == file2.Id));
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldUpdateFile_WhenFileExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            // Update the file
            file.Name = "updated.txt";
            file.Status = IndexingStatus.Indexed;

            // Act
            await _fileStore.UpdateAsync(file);

            // Assert
            KnowledgeFile? retrieved = await _fileStore.GetAsync(file.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("updated.txt", retrieved.Name);
            Assert.AreEqual(IndexingStatus.Indexed, retrieved.Status);
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldThrow_WhenFileIsNull()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _fileStore.UpdateAsync(null!));
        }

        [TestMethod]
        public async Task UpdateAsync_ShouldThrow_WhenFileDoesNotExist()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _fileStore.UpdateAsync(file));
        }

        [TestMethod]
        public async Task DeleteAsync_ShouldReturnTrue_WhenFileExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            // Act
            bool result = await _fileStore.DeleteAsync(file.Id);

            // Assert
            Assert.IsTrue(result);
            KnowledgeFile? retrieved = await _fileStore.GetAsync(file.Id);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task DeleteAsync_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _fileStore.DeleteAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetAllAsync_ShouldReturnFilesInCorrectOrder()
        {
            // Arrange
            KnowledgeFile file1 = new KnowledgeFile(Guid.NewGuid(), "test1.txt", new byte[] { 1, 2, 3, 4 })
            {
                ProcessedAt = DateTime.UtcNow.AddHours(-1)
            };
            KnowledgeFile file2 = new KnowledgeFile(Guid.NewGuid(), "test2.txt", new byte[] { 5, 6, 7, 8 })
            {
                ProcessedAt = DateTime.UtcNow
            };
            
            await _fileStore.AddAsync(file1);
            await _fileStore.AddAsync(file2);

            // Act
            IEnumerable<KnowledgeFile> result = await _fileStore.GetAllAsync();

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFile> files = result.ToList();
            Assert.AreEqual(2, files.Count);
            // Should be ordered by processed_at DESC, so file2 should come first
            Assert.AreEqual(file2.Id, files[0].Id);
            Assert.AreEqual(file1.Id, files[1].Id);
        }
    }
}

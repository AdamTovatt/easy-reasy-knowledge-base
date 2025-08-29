using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.Storage.Postgres;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    [TestClass]
    public sealed class PostgresChunkStoreTests
    {
        private static string _connectionString = string.Empty;
        private static PostgresChunkStore _chunkStore = null!;
        private static PostgresFileStore _fileStore = null!;
        private static PostgresSectionStore _sectionStore = null!;
        private static IDbConnectionFactory _connectionFactory = null!;
        private static ILogger _logger = null!;

        [ClassInitialize]
        public static void BeforeAll(TestContext testContext)
        {
            try
            {
                // Use a test database connection string
                _connectionString = TestDatabaseHelper.GetConnectionString();
                _connectionFactory = new PostgresConnectionFactory(_connectionString);
                _fileStore = new PostgresFileStore(_connectionFactory);
                _chunkStore = new PostgresChunkStore(_connectionFactory);
                _sectionStore = new PostgresSectionStore(_connectionFactory, _chunkStore);
                
                // Create a simple logger for test output
                _logger = TestDatabaseHelper.CreateLogger<PostgresChunkStoreTests>();
                
                // Ensure database is clean and migrated
                TestDatabaseHelper.SetupDatabase<PostgresChunkStoreTests>();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize PostgreSQL test environment: {exception.Message}");
                Assert.Inconclusive("Failed to initialize PostgreSQL test environment for integration tests.");
            }
        }

        [ClassCleanup]
        public static void AfterAll()
        {
            _chunkStore = null!;
            _sectionStore = null!;
            _fileStore = null!;
            _connectionFactory = null!;
            _logger = null!;
            
            // Clean up the test database
            TestDatabaseHelper.CleanupDatabase<PostgresChunkStoreTests>();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensure database is clean for each test
            TestDatabaseHelper.SetupDatabase<PostgresChunkStoreTests>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up after each test
            TestDatabaseHelper.CleanupDatabase<PostgresChunkStoreTests>();
        }

        /// <summary>
        /// Helper method to create a valid section with chunks for testing.
        /// </summary>
        private async Task<KnowledgeFileSection> CreateValidSectionAsync(Guid fileId, int sectionIndex, string summary = "Test section")
        {
            await Task.CompletedTask;

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                $"Test chunk content for section {sectionIndex}",
                new float[] { 0.1f, 0.2f, 0.3f }
            );

            List<KnowledgeFileChunk> chunks = new List<KnowledgeFileChunk> { chunk };

            // Create section using the CreateFromChunks method
            return KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex);
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenConnectionFactoryIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresChunkStore(null!));
        }

        [TestMethod]
        public void Constructor_ShouldAccept_WhenConnectionFactoryIsValid()
        {
            // Act & Assert
            PostgresChunkStore store = new PostgresChunkStore(_connectionFactory);
            Assert.IsNotNull(store);
        }

        [TestMethod]
        public async Task AddAsync_ShouldAddChunk_WhenValidChunkProvided()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content",
                new float[] { 0.1f, 0.2f, 0.3f }
            );

            // Act
            await _chunkStore.AddAsync(chunk);

            // Assert
            KnowledgeFileChunk? retrieved = await _chunkStore.GetAsync(chunk.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(chunk.Id, retrieved.Id);
            Assert.AreEqual(chunk.SectionId, retrieved.SectionId);
            Assert.AreEqual(chunk.Content, retrieved.Content);
            Assert.IsNotNull(retrieved.Embedding);
            CollectionAssert.AreEqual(chunk.Embedding!, retrieved.Embedding!);
        }

        [TestMethod]
        public async Task AddAsync_ShouldThrow_WhenChunkIsNull()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _chunkStore.AddAsync(null!));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnChunk_WhenChunkExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            await _chunkStore.AddAsync(chunk);

            // Act
            KnowledgeFileChunk? result = await _chunkStore.GetAsync(chunk.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(chunk.Id, result.Id);
            Assert.AreEqual(chunk.SectionId, result.SectionId);
            Assert.AreEqual(chunk.Content, result.Content);
            Assert.IsNotNull(result.Embedding);
            CollectionAssert.AreEqual(chunk.Embedding!, result.Embedding!);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnNull_WhenChunkDoesNotExist()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            KnowledgeFileChunk? result = await _chunkStore.GetAsync(nonExistentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnMultipleChunks_WhenMultipleIdsProvided()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content 1",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                1,
                "Test chunk content 2",
                new float[] { 0.4f, 0.5f, 0.6f }
            );

            await _chunkStore.AddAsync(chunk1);
            await _chunkStore.AddAsync(chunk2);

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetAsync(new[] { chunk1.Id, chunk2.Id });

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFileChunk> chunks = result.ToList();
            Assert.AreEqual(2, chunks.Count);
            Assert.IsTrue(chunks.Any(c => c.Id == chunk1.Id));
            Assert.IsTrue(chunks.Any(c => c.Id == chunk2.Id));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnEmptyCollection_WhenNoIdsProvided()
        {
            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetAsync(Enumerable.Empty<Guid>());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnEmptyCollection_WhenNullIdsProvided()
        {
            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetAsync(null!);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetByIndexAsync_ShouldReturnChunk_WhenChunkExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            await _chunkStore.AddAsync(chunk);

            // Act
            KnowledgeFileChunk? result = await _chunkStore.GetByIndexAsync(sectionId, 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(chunk.Id, result.Id);
            Assert.AreEqual(sectionId, result.SectionId);
            Assert.AreEqual(0, result.ChunkIndex);
        }

        [TestMethod]
        public async Task GetByIndexAsync_ShouldReturnNull_WhenChunkDoesNotExist()
        {
            // Arrange
            Guid sectionId = Guid.NewGuid();

            // Act
            KnowledgeFileChunk? result = await _chunkStore.GetByIndexAsync(sectionId, 0);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetBySectionAsync_ShouldReturnChunks_WhenChunksExist()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content 1",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                1,
                "Test chunk content 2",
                new float[] { 0.4f, 0.5f, 0.6f }
            );

            await _chunkStore.AddAsync(chunk1);
            await _chunkStore.AddAsync(chunk2);

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetBySectionAsync(sectionId);

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFileChunk> chunks = result.ToList();
            Assert.AreEqual(2, chunks.Count);
            Assert.AreEqual(0, chunks[0].ChunkIndex);
            Assert.AreEqual(1, chunks[1].ChunkIndex);
        }

        [TestMethod]
        public async Task GetBySectionAsync_ShouldReturnEmptyCollection_WhenNoChunksExist()
        {
            // Arrange
            Guid sectionId = Guid.NewGuid();

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetBySectionAsync(sectionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task DeleteByFileAsync_ShouldReturnTrue_WhenChunksExist()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            await _chunkStore.AddAsync(chunk);

            // Act
            bool result = await _chunkStore.DeleteByFileAsync(file.Id);

            // Assert
            Assert.IsTrue(result);
            KnowledgeFileChunk? retrieved = await _chunkStore.GetAsync(chunk.Id);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task DeleteByFileAsync_ShouldReturnFalse_WhenNoChunksExist()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();

            // Act
            bool result = await _chunkStore.DeleteByFileAsync(fileId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AddAsync_ShouldHandleChunkWithoutEmbedding()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content without embedding"
            );

            // Act
            await _chunkStore.AddAsync(chunk);

            // Assert
            KnowledgeFileChunk? retrieved = await _chunkStore.GetAsync(chunk.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(chunk.Id, retrieved.Id);
            Assert.AreEqual(chunk.Content, retrieved.Content);
            Assert.IsNull(retrieved.Embedding);
        }

        [TestMethod]
        public async Task GetBySectionAsync_ShouldReturnChunksInOrder()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            Guid sectionId = Guid.NewGuid();
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks in reverse order
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                2,
                "Test chunk content 2"
            );
            KnowledgeFileChunk chunk0 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                "Test chunk content 0"
            );
            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                1,
                "Test chunk content 1"
            );

            await _chunkStore.AddAsync(chunk2);
            await _chunkStore.AddAsync(chunk0);
            await _chunkStore.AddAsync(chunk1);

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetBySectionAsync(sectionId);

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFileChunk> chunks = result.ToList();
            Assert.AreEqual(3, chunks.Count);
            Assert.AreEqual(0, chunks[0].ChunkIndex);
            Assert.AreEqual(1, chunks[1].ChunkIndex);
            Assert.AreEqual(2, chunks[2].ChunkIndex);
        }
    }
}

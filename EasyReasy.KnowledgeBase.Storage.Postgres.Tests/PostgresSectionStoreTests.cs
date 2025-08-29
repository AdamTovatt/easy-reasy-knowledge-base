using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.Storage.Postgres;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    [TestClass]
    public sealed class PostgresSectionStoreTests
    {
        private static string _connectionString = string.Empty;
        private static PostgresSectionStore _sectionStore = null!;
        private static PostgresFileStore _fileStore = null!;
        private static PostgresChunkStore _chunkStore = null!;
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
                _logger = TestDatabaseHelper.CreateLogger<PostgresSectionStoreTests>();
                
                // Ensure database is clean and migrated
                TestDatabaseHelper.SetupDatabase<PostgresSectionStoreTests>();
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
            _sectionStore = null!;
            _fileStore = null!;
            _chunkStore = null!;
            _connectionFactory = null!;
            _logger = null!;
            
            // Clean up the test database
            TestDatabaseHelper.CleanupDatabase<PostgresSectionStoreTests>();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensure database is clean for each test
            TestDatabaseHelper.SetupDatabase<PostgresSectionStoreTests>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up after each test
            TestDatabaseHelper.CleanupDatabase<PostgresSectionStoreTests>();
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
            KnowledgeFileSection result = KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex);
            result.Summary = summary;

            return result;
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenConnectionFactoryIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSectionStore(null!, _chunkStore));
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenChunkStoreIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresSectionStore(_connectionFactory, null!));
        }

        [TestMethod]
        public void Constructor_ShouldAccept_WhenParametersAreValid()
        {
            // Act & Assert
            PostgresSectionStore store = new PostgresSectionStore(_connectionFactory, _chunkStore);
            Assert.IsNotNull(store);
        }

        [TestMethod]
        public async Task AddAsync_ShouldAddSection_WhenValidSectionProvided()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);

            // Act
            await _sectionStore.AddAsync(section);

            // Assert
            KnowledgeFileSection? retrieved = await _sectionStore.GetAsync(section.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(section.Id, retrieved.Id);
            Assert.AreEqual(section.FileId, retrieved.FileId);
            Assert.AreEqual(section.SectionIndex, retrieved.SectionIndex);
            Assert.AreEqual(section.Summary, retrieved.Summary);
        }

        [TestMethod]
        public async Task AddAsync_ShouldThrow_WhenSectionIsNull()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _sectionStore.AddAsync(null!));
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnSection_WhenSectionExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetAsync(section.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(section.Id, result.Id);
            Assert.AreEqual(section.FileId, result.FileId);
            Assert.AreEqual(section.SectionIndex, result.SectionIndex);
            Assert.AreEqual(section.Summary, result.Summary);
            Assert.AreEqual(section.Chunks.Count, result.Chunks.Count);
        }

        [TestMethod]
        public async Task GetAsync_ShouldReturnNull_WhenSectionDoesNotExist()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetAsync(nonExistentId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByIndexAsync_ShouldReturnSection_WhenSectionExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetByIndexAsync(file.Id, 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(section.Id, result.Id);
            Assert.AreEqual(file.Id, result.FileId);
            Assert.AreEqual(0, result.SectionIndex);
            Assert.AreEqual(section.Summary, result.Summary);
        }

        [TestMethod]
        public async Task GetByIndexAsync_ShouldReturnNull_WhenSectionDoesNotExist()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetByIndexAsync(fileId, 0);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task DeleteByFileAsync_ShouldReturnTrue_WhenSectionsExist()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            bool result = await _sectionStore.DeleteByFileAsync(file.Id);

            // Assert
            Assert.IsTrue(result);
            KnowledgeFileSection? retrieved = await _sectionStore.GetAsync(section.Id);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task DeleteByFileAsync_ShouldReturnFalse_WhenNoSectionsExist()
        {
            // Arrange
            Guid fileId = Guid.NewGuid();

            // Act
            bool result = await _sectionStore.DeleteByFileAsync(fileId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AddAsync_ShouldHandleSectionWithNullSummary()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            section.Summary = null;

            // Act
            await _sectionStore.AddAsync(section);

            // Assert
            KnowledgeFileSection? retrieved = await _sectionStore.GetAsync(section.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(section.Id, retrieved.Id);
            Assert.IsNull(retrieved.Summary);
        }

        [TestMethod]
        public async Task AddAsync_ShouldHandleSectionWithNullAdditionalContext()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            section.AdditionalContext = null;

            // Act
            await _sectionStore.AddAsync(section);

            // Assert
            KnowledgeFileSection? retrieved = await _sectionStore.GetAsync(section.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(section.Id, retrieved.Id);
            Assert.IsNull(retrieved.AdditionalContext);
        }

        [TestMethod]
        public async Task GetAsync_ShouldLoadChunksForSection()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetAsync(section.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(section.Chunks.Count, result.Chunks.Count);
            
            // Verify the chunks are loaded correctly
            for (int i = 0; i < section.Chunks.Count; i++)
            {
                Assert.AreEqual(section.Chunks[i].Id, result.Chunks[i].Id);
                Assert.AreEqual(section.Chunks[i].Content, result.Chunks[i].Content);
            }
        }

        [TestMethod]
        public async Task GetByIndexAsync_ShouldLoadChunksForSection()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            KnowledgeFileSection? result = await _sectionStore.GetByIndexAsync(file.Id, 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(section.Chunks.Count, result.Chunks.Count);
            
            // Verify the chunks are loaded correctly
            for (int i = 0; i < section.Chunks.Count; i++)
            {
                Assert.AreEqual(section.Chunks[i].Id, result.Chunks[i].Id);
                Assert.AreEqual(section.Chunks[i].Content, result.Chunks[i].Content);
            }
        }

        [TestMethod]
        public async Task DeleteByFileAsync_ShouldDeleteAllSectionsForFile()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section1 = await CreateValidSectionAsync(file.Id, 0, "Section 1");
            KnowledgeFileSection section2 = await CreateValidSectionAsync(file.Id, 1, "Section 2");
            
            await _sectionStore.AddAsync(section1);
            await _sectionStore.AddAsync(section2);

            // Add chunks for both sections
            foreach (KnowledgeFileChunk chunk in section1.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }
            foreach (KnowledgeFileChunk chunk in section2.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Act
            bool result = await _sectionStore.DeleteByFileAsync(file.Id);

            // Assert
            Assert.IsTrue(result);
            KnowledgeFileSection? retrieved1 = await _sectionStore.GetAsync(section1.Id);
            KnowledgeFileSection? retrieved2 = await _sectionStore.GetAsync(section2.Id);
            Assert.IsNull(retrieved1);
            Assert.IsNull(retrieved2);
        }
    }
}

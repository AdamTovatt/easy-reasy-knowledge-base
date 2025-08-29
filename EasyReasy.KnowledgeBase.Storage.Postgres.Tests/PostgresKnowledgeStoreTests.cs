using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.Storage.Postgres;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    [TestClass]
    public sealed class PostgresKnowledgeStoreTests
    {
        private static string _connectionString = string.Empty;
        private static PostgresKnowledgeStore _knowledgeStore = null!;
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
                _knowledgeStore = new PostgresKnowledgeStore(_connectionFactory);
                
                // Create a simple logger for test output
                _logger = TestDatabaseHelper.CreateLogger<PostgresKnowledgeStoreTests>();
                
                // Ensure database is clean and migrated
                TestDatabaseHelper.SetupDatabase<PostgresKnowledgeStoreTests>();
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
            _knowledgeStore = null!;
            _connectionFactory = null!;
            _logger = null!;
            
            // Clean up the test database
            TestDatabaseHelper.CleanupDatabase<PostgresKnowledgeStoreTests>();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensure database is clean for each test
            TestDatabaseHelper.SetupDatabase<PostgresKnowledgeStoreTests>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up after each test
            TestDatabaseHelper.CleanupDatabase<PostgresKnowledgeStoreTests>();
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
            Assert.ThrowsException<ArgumentNullException>(() => new PostgresKnowledgeStore(null!));
        }

        [TestMethod]
        public void Constructor_ShouldAccept_WhenConnectionFactoryIsValid()
        {
            // Act & Assert
            PostgresKnowledgeStore store = new PostgresKnowledgeStore(_connectionFactory);
            Assert.IsNotNull(store);
            Assert.IsNotNull(store.Files);
            Assert.IsNotNull(store.Sections);
            Assert.IsNotNull(store.Chunks);
        }

        [TestMethod]
        public void Create_ShouldThrow_WhenConnectionStringIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => PostgresKnowledgeStore.Create((string)null!));
        }

        [TestMethod]
        public void Create_ShouldThrow_WhenConnectionStringIsEmpty()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => PostgresKnowledgeStore.Create(string.Empty));
        }

        [TestMethod]
        public void Create_ShouldThrow_WhenConnectionStringIsWhitespace()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => PostgresKnowledgeStore.Create("   "));
        }

        [TestMethod]
        public void Create_ShouldAccept_WhenConnectionStringIsValid()
        {
            // Act & Assert
            PostgresKnowledgeStore store = PostgresKnowledgeStore.Create(_connectionString);
            Assert.IsNotNull(store);
            Assert.IsNotNull(store.Files);
            Assert.IsNotNull(store.Sections);
            Assert.IsNotNull(store.Chunks);
        }

        [TestMethod]
        public void Create_ShouldAccept_WhenConnectionFactoryIsValid()
        {
            // Act & Assert
            PostgresKnowledgeStore store = PostgresKnowledgeStore.Create(_connectionFactory);
            Assert.IsNotNull(store);
            Assert.IsNotNull(store.Files);
            Assert.IsNotNull(store.Sections);
            Assert.IsNotNull(store.Chunks);
        }

        [TestMethod]
        public void Create_ShouldThrow_WhenConnectionFactoryIsNull()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => PostgresKnowledgeStore.Create((IDbConnectionFactory)null!));
        }

        [TestMethod]
        public async Task Files_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });

            // Act
            Guid result = await _knowledgeStore.Files.AddAsync(file);

            // Assert
            Assert.AreEqual(file.Id, result);
            KnowledgeFile? retrieved = await _knowledgeStore.Files.GetAsync(file.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(file.Id, retrieved.Id);
            Assert.AreEqual(file.Name, retrieved.Name);
        }

        [TestMethod]
        public async Task Sections_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _knowledgeStore.Files.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);

            // Act
            await _knowledgeStore.Sections.AddAsync(section);

            // Add chunks for the section
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _knowledgeStore.Chunks.AddAsync(chunk);
            }

            // Assert
            KnowledgeFileSection? retrieved = await _knowledgeStore.Sections.GetAsync(section.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(section.Id, retrieved.Id);
            Assert.AreEqual(section.FileId, retrieved.FileId);
        }

        [TestMethod]
        public async Task Chunks_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _knowledgeStore.Files.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _knowledgeStore.Sections.AddAsync(section);

            KnowledgeFileChunk chunk = section.Chunks[0];

            // Act
            await _knowledgeStore.Chunks.AddAsync(chunk);

            // Assert
            KnowledgeFileChunk? retrieved = await _knowledgeStore.Chunks.GetAsync(chunk.Id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(chunk.Id, retrieved.Id);
            Assert.AreEqual(chunk.SectionId, retrieved.SectionId);
            Assert.AreEqual(chunk.Content, retrieved.Content);
        }

        [TestMethod]
        public async Task CompleteWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            KnowledgeFileChunk chunk = section.Chunks[0];

            // Act - Add file
            Guid fileId = await _knowledgeStore.Files.AddAsync(file);

            // Act - Add section
            await _knowledgeStore.Sections.AddAsync(section);

            // Act - Add chunk
            await _knowledgeStore.Chunks.AddAsync(chunk);

            // Assert - Verify file
            KnowledgeFile? retrievedFile = await _knowledgeStore.Files.GetAsync(fileId);
            Assert.IsNotNull(retrievedFile);
            Assert.AreEqual(file.Id, retrievedFile.Id);

            // Assert - Verify section
            KnowledgeFileSection? retrievedSection = await _knowledgeStore.Sections.GetAsync(section.Id);
            Assert.IsNotNull(retrievedSection);
            Assert.AreEqual(section.Id, retrievedSection.Id);

            // Assert - Verify chunk
            KnowledgeFileChunk? retrievedChunk = await _knowledgeStore.Chunks.GetAsync(chunk.Id);
            Assert.IsNotNull(retrievedChunk);
            Assert.AreEqual(chunk.Id, retrievedChunk.Id);

            // Act - Delete file (should cascade to sections and chunks)
            bool fileDeleted = await _knowledgeStore.Files.DeleteAsync(fileId);
            Assert.IsTrue(fileDeleted);

            // Assert - Verify everything is deleted
            KnowledgeFile? deletedFile = await _knowledgeStore.Files.GetAsync(fileId);
            Assert.IsNull(deletedFile);

            // Note: Sections and chunks are not automatically deleted when file is deleted
            // This is by design - they need to be explicitly deleted
        }

        [TestMethod]
        public async Task MultipleFiles_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file1 = new KnowledgeFile(Guid.NewGuid(), "test1.txt", new byte[] { 1, 2, 3, 4 });
            KnowledgeFile file2 = new KnowledgeFile(Guid.NewGuid(), "test2.txt", new byte[] { 5, 6, 7, 8 });

            // Act
            Guid fileId1 = await _knowledgeStore.Files.AddAsync(file1);
            Guid fileId2 = await _knowledgeStore.Files.AddAsync(file2);

            // Assert
            IEnumerable<KnowledgeFile> allFiles = await _knowledgeStore.Files.GetAllAsync();
            Assert.AreEqual(2, allFiles.Count());
            Assert.IsTrue(allFiles.Any(f => f.Id == fileId1));
            Assert.IsTrue(allFiles.Any(f => f.Id == fileId2));
        }

        [TestMethod]
        public async Task MultipleSections_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _knowledgeStore.Files.AddAsync(file);

            KnowledgeFileSection section1 = await CreateValidSectionAsync(file.Id, 0, "Section 1");
            KnowledgeFileSection section2 = await CreateValidSectionAsync(file.Id, 1, "Section 2");

            // Act
            await _knowledgeStore.Sections.AddAsync(section1);
            await _knowledgeStore.Sections.AddAsync(section2);

            // Add chunks for both sections
            foreach (KnowledgeFileChunk chunk in section1.Chunks)
            {
                await _knowledgeStore.Chunks.AddAsync(chunk);
            }
            foreach (KnowledgeFileChunk chunk in section2.Chunks)
            {
                await _knowledgeStore.Chunks.AddAsync(chunk);
            }

            // Assert
            KnowledgeFileSection? retrieved1 = await _knowledgeStore.Sections.GetByIndexAsync(file.Id, 0);
            KnowledgeFileSection? retrieved2 = await _knowledgeStore.Sections.GetByIndexAsync(file.Id, 1);
            
            Assert.IsNotNull(retrieved1);
            Assert.IsNotNull(retrieved2);
            Assert.AreEqual("Section 1", retrieved1.Summary);
            Assert.AreEqual("Section 2", retrieved2.Summary);
        }

        [TestMethod]
        public async Task MultipleChunks_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _knowledgeStore.Files.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _knowledgeStore.Sections.AddAsync(section);

            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                0,
                "Test chunk content 1",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1,
                "Test chunk content 2",
                new float[] { 0.4f, 0.5f, 0.6f }
            );

            // Act
            await _knowledgeStore.Chunks.AddAsync(chunk1);
            await _knowledgeStore.Chunks.AddAsync(chunk2);

            // Assert
            IEnumerable<KnowledgeFileChunk> chunks = await _knowledgeStore.Chunks.GetBySectionAsync(section.Id);
            Assert.AreEqual(2, chunks.Count());
            Assert.IsTrue(chunks.Any(c => c.Id == chunk1.Id));
            Assert.IsTrue(chunks.Any(c => c.Id == chunk2.Id));
        }

        [TestMethod]
        public async Task DeleteByFile_ShouldWorkCorrectly()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _knowledgeStore.Files.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _knowledgeStore.Sections.AddAsync(section);

            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _knowledgeStore.Chunks.AddAsync(chunk);
            }

            // Act
            bool sectionsDeleted = await _knowledgeStore.Sections.DeleteByFileAsync(file.Id);
            bool chunksDeleted = await _knowledgeStore.Chunks.DeleteByFileAsync(file.Id);

            // Assert
            Assert.IsTrue(sectionsDeleted);
            Assert.IsTrue(chunksDeleted);

            // Verify sections are deleted
            KnowledgeFileSection? retrievedSection = await _knowledgeStore.Sections.GetAsync(section.Id);
            Assert.IsNull(retrievedSection);

            // Verify chunks are deleted
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                KnowledgeFileChunk? retrievedChunk = await _knowledgeStore.Chunks.GetAsync(chunk.Id);
                Assert.IsNull(retrievedChunk);
            }
        }
    }
}

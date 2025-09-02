using EasyReasy.KnowledgeBase.Models;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    [TestClass]
    public sealed class PostgresChunkStoreTests : PostgresTestBase<PostgresChunkStoreTests>
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since SaveSectionWithChunksAsync already saved a chunk at index 0
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since SaveSectionWithChunksAsync already saved a chunk at index 0
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since SaveSectionWithChunksAsync already saved a chunk at index 0
                "Test chunk content 1",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                2, // Use index 2
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since CreateValidSectionAsync already creates a chunk at index 0
                "Test chunk content",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            await _chunkStore.AddAsync(chunk);

            // Act
            KnowledgeFileChunk? result = await _chunkStore.GetByIndexAsync(section.Id, 1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(chunk.Id, result.Id);
            Assert.AreEqual(section.Id, result.SectionId);
            Assert.AreEqual(1, result.ChunkIndex);
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since CreateValidSectionAsync already creates a chunk at index 0
                "Test chunk content 1",
                new float[] { 0.1f, 0.2f, 0.3f }
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                2, // Use index 2
                "Test chunk content 2",
                new float[] { 0.4f, 0.5f, 0.6f }
            );

            await _chunkStore.AddAsync(chunk1);
            await _chunkStore.AddAsync(chunk2);

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetBySectionAsync(section.Id);

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFileChunk> chunks = result.ToList();
            Assert.AreEqual(3, chunks.Count); // 1 from CreateValidSectionAsync + 2 new ones
            Assert.AreEqual(0, chunks[0].ChunkIndex); // Original from CreateValidSectionAsync
            Assert.AreEqual(1, chunks[1].ChunkIndex); // chunk1
            Assert.AreEqual(2, chunks[2].ChunkIndex); // chunk2
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since SaveSectionWithChunksAsync already saved a chunk at index 0
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1, // Use index 1 since SaveSectionWithChunksAsync already saved a chunk at index 0
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

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await SaveSectionWithChunksAsync(section);

            // Add chunks in reverse order (avoid index 0 since CreateValidSectionAsync already uses it)
            KnowledgeFileChunk chunk3 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                3,
                "Test chunk content 3"
            );
            KnowledgeFileChunk chunk1 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                1,
                "Test chunk content 1"
            );
            KnowledgeFileChunk chunk2 = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                2,
                "Test chunk content 2"
            );

            await _chunkStore.AddAsync(chunk3);
            await _chunkStore.AddAsync(chunk1);
            await _chunkStore.AddAsync(chunk2);

            // Act
            IEnumerable<KnowledgeFileChunk> result = await _chunkStore.GetBySectionAsync(section.Id);

            // Assert
            Assert.IsNotNull(result);
            List<KnowledgeFileChunk> chunks = result.ToList();
            Assert.AreEqual(4, chunks.Count); // 1 from CreateValidSectionAsync + 3 new ones
            Assert.AreEqual(0, chunks[0].ChunkIndex); // Original from CreateValidSectionAsync
            Assert.AreEqual(1, chunks[1].ChunkIndex); // chunk1
            Assert.AreEqual(2, chunks[2].ChunkIndex); // chunk2
            Assert.AreEqual(3, chunks[3].ChunkIndex); // chunk3
        }

        [TestMethod]
        public async Task AddAsync_ShouldThrowConstraintViolation_WhenDuplicateChunkIndexExists()
        {
            // Arrange
            KnowledgeFile file = new KnowledgeFile(Guid.NewGuid(), "test.txt", new byte[] { 1, 2, 3, 4 });
            await _fileStore.AddAsync(file);

            KnowledgeFileSection section = await CreateValidSectionAsync(file.Id, 0);
            await _sectionStore.AddAsync(section);

            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }

            // Try to add another chunk at the same index
            KnowledgeFileChunk duplicateChunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                section.Id,
                0, // Same index as the existing chunk from CreateValidSectionAsync
                "Duplicate chunk content"
            );

            // Act & Assert - Should throw constraint violation
            await Assert.ThrowsExceptionAsync<Npgsql.PostgresException>(() => _chunkStore.AddAsync(duplicateChunk));
        }
    }
}

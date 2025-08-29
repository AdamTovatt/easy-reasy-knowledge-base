using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.Storage.Postgres;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace EasyReasy.KnowledgeBase.Storage.Postgres.Tests
{
    /// <summary>
    /// Base class for PostgreSQL storage tests that provides common setup, stores, and lifecycle management.
    /// </summary>
    /// <typeparam name="TTestClass">The test class type for logging and database setup.</typeparam>
    public abstract class PostgresTestBase<TTestClass> where TTestClass : class
    {
        protected static string _connectionString = string.Empty;
        protected static PostgresFileStore _fileStore = null!;
        protected static PostgresChunkStore _chunkStore = null!;
        protected static PostgresSectionStore _sectionStore = null!;
        protected static PostgresKnowledgeStore _knowledgeStore = null!;
        protected static IDbConnectionFactory _connectionFactory = null!;
        protected static ILogger _logger = null!;

        /// <summary>
        /// Initializes the test environment with database connections and stores.
        /// </summary>
        protected static void InitializeTestEnvironment()
        {
            try
            {
                // Use a test database connection string
                _connectionString = TestDatabaseHelper.GetConnectionString();
                _connectionFactory = new PostgresConnectionFactory(_connectionString);
                _fileStore = new PostgresFileStore(_connectionFactory);
                _chunkStore = new PostgresChunkStore(_connectionFactory);
                _sectionStore = new PostgresSectionStore(_connectionFactory, _chunkStore);
                _knowledgeStore = new PostgresKnowledgeStore(_connectionFactory);
                
                // Create a simple logger for test output
                _logger = TestDatabaseHelper.CreateLogger<TTestClass>();
                
                // Ensure database is clean and migrated
                TestDatabaseHelper.SetupDatabase<TTestClass>();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize PostgreSQL test environment: {exception.Message}");
                Assert.Inconclusive("Failed to initialize PostgreSQL test environment for integration tests.");
            }
        }

        /// <summary>
        /// Cleans up the test environment and disposes of resources.
        /// </summary>
        protected static void CleanupTestEnvironment()
        {
            _fileStore = null!;
            _chunkStore = null!;
            _sectionStore = null!;
            _knowledgeStore = null!;
            _connectionFactory = null!;
            _logger = null!;
            
            // Clean up the test database
            TestDatabaseHelper.CleanupDatabase<TTestClass>();
        }

        /// <summary>
        /// Sets up the database for each individual test.
        /// </summary>
        protected static void SetupDatabaseForTest()
        {
            // Just cleanup between tests - migrations already ran during initialization
            TestDatabaseHelper.CleanupDatabase<TTestClass>();
        }

        /// <summary>
        /// Cleans up the database after each individual test.
        /// </summary>
        protected static void CleanupDatabaseAfterTest()
        {
            TestDatabaseHelper.CleanupDatabase<TTestClass>();
        }

        /// <summary>
        /// Helper method to create a valid section with chunks for testing.
        /// </summary>
        protected static async Task<KnowledgeFileSection> CreateValidSectionAsync(Guid fileId, int sectionIndex, string summary = "Test section")
        {
            await Task.CompletedTask;

            // Create the section first with a new ID
            Guid sectionId = Guid.NewGuid();
            
            // Create chunk with the correct section ID
            KnowledgeFileChunk chunk = new KnowledgeFileChunk(
                Guid.NewGuid(),
                sectionId,
                0,
                $"Test chunk content for section {sectionIndex}",
                new float[] { 0.1f, 0.2f, 0.3f }
            );

            List<KnowledgeFileChunk> chunks = new List<KnowledgeFileChunk> { chunk };

            // Create section with the same ID that the chunk references
            KnowledgeFileSection result = new KnowledgeFileSection(sectionId, fileId, sectionIndex, chunks, summary);

            return result;
        }

        /// <summary>
        /// Helper method to save a section and its chunks to the database in the correct order.
        /// </summary>
        protected static async Task SaveSectionWithChunksAsync(KnowledgeFileSection section)
        {
            // Save the section first
            await _sectionStore.AddAsync(section);

            // Then save each chunk
            foreach (KnowledgeFileChunk chunk in section.Chunks)
            {
                await _chunkStore.AddAsync(chunk);
            }
        }
    }
}

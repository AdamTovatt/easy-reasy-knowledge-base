using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.BertTokenization;
using EasyReasy.KnowledgeBase.Chunking;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.Models;
using EasyReasy.KnowledgeBase.OllamaGeneration;
using EasyReasy.KnowledgeBase.Tests.TestUtilities;
using System.Diagnostics;
using System.Reflection;

namespace EasyReasy.KnowledgeBase.Tests.Chunking
{
    [TestClass]
    public class SectionReaderIntegrationTests
    {
        private static ResourceManager _resourceManager = null!;
        private static ITokenizer _tokenizer = null!;
        private static IEmbeddingService? _ollamaEmbeddingService = null;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext testContext)
        {
            _resourceManager = await ResourceManager.CreateInstanceAsync(Assembly.GetExecutingAssembly());
            _tokenizer = await BertTokenizer.CreateAsync();

            // Load environment variables from test configuration file
            try
            {
                EnvironmentVariableHelper.LoadVariablesFromFile("..\\..\\TestEnvironmentVariables.txt");
                EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(OllamaTestEnvironmentVariables));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Could not load TestEnvironmentVariables.txt: {exception.Message}");
                Assert.Inconclusive();
            }

            _ollamaEmbeddingService = await EasyReasyOllamaEmbeddingService.CreateAsync(
                OllamaTestEnvironmentVariables.OllamaBaseUrl.GetValue(),
                OllamaTestEnvironmentVariables.OllamaApiKey.GetValue(),
                OllamaTestEnvironmentVariables.OllamaEmbeddingModelName.GetValue());
        }

        [ClassCleanup]
        public static void AfterAll()
        {
            if (_ollamaEmbeddingService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [TestMethod]
        public async Task ReadSectionsAsync_WithRealEmbeddings_ShouldHandleCancellation()
        {
            // Skip test if Ollama service is not available
            if (_ollamaEmbeddingService == null)
            {
                Assert.Inconclusive("Ollama embedding service not available. Set environment variables to run integration tests.");
                return;
            }

            const int rangeMax = 100;

            // Arrange
            string content = "# Test Document\n\n" + string.Join("\n\n", Enumerable.Range(1, rangeMax).Select(i => $"Paragraph {i}."));
            Console.WriteLine("=== Integration Test: Cancellation with Real Embeddings ===");
            Console.WriteLine($"Input content: {content.Length} characters with {rangeMax} paragraphs");

            Guid fileId = Guid.NewGuid();
            using Stream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            SectionReaderFactory factory = new SectionReaderFactory(_ollamaEmbeddingService, _tokenizer);
            SectionReader sectionReader = factory.CreateForMarkdown(stream, fileId, maxTokensPerChunk: 20, maxTokensPerSection: 200);

            const int cancellationTimeoutMs = 200; // 200 ms for real embeddings

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(cancellationTimeoutMs));
            Stopwatch stopwatch = Stopwatch.StartNew();

            bool didHandleException = false;
            int sectionsProcessed = 0;

            // Act & Assert
            Console.WriteLine($"Starting processing with {cancellationTimeoutMs}ms cancellation timeout...");
            try
            {
                await foreach (List<KnowledgeFileChunk> section in sectionReader.ReadSectionsAsync(cancellationTokenSource.Token))
                {
                    sectionsProcessed++;
                    Console.WriteLine($"Processed section {sectionsProcessed} with {section.Count} chunks");
                }
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                Console.WriteLine("Exception occurred:");
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.GetType().Name);

                if (exception is OperationCanceledException)
                {
                    didHandleException = true;
                }
            }

            stopwatch.Stop();

            Console.WriteLine($"Cancellation was expected after {cancellationTimeoutMs}ms");
            Console.WriteLine($"Actual time to reach post cancellation code: {stopwatch.ElapsedMilliseconds}ms");

            Assert.IsTrue(didHandleException, "Should handle cancellation exception");
        }

        [TestMethod]
        public async Task ReadSectionsAsync_TestDocument3()
        {
            Assert.IsNotNull(_ollamaEmbeddingService);

            // Arrange - Use real test document to test similarity-based grouping
            Guid fileId = Guid.NewGuid();
            using Stream stream = await _resourceManager.GetResourceStreamAsync(TestDataFiles.TestDocument03);
            SectionReaderFactory factory = new SectionReaderFactory(_ollamaEmbeddingService, _tokenizer);
            SectionReader sectionReader = factory.CreateForMarkdown(stream, fileId, maxTokensPerChunk: 100, maxTokensPerSection: 1000);

            // Act
            List<List<KnowledgeFileChunk>> sections = new List<List<KnowledgeFileChunk>>();
            int sectionIndex = 0;
            await foreach (List<KnowledgeFileChunk> chunks in sectionReader.ReadSectionsAsync())
            {
                Console.WriteLine("=== SECTION START ===");
                Console.WriteLine(KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex).ToString("\n-------------\n"));
                int tokenCount = _tokenizer.CountTokens(KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex).ToString());
                Console.WriteLine($"=== SECTION ENDED WITH {tokenCount} TOKENS ===");
                sectionIndex++;
            }
        }

        [TestMethod]
        public async Task ReadSectionsAsync_TestDocument4()
        {
            Assert.IsNotNull(_ollamaEmbeddingService);

            // Arrange - Use real test document to test similarity-based grouping
            Guid fileId = Guid.NewGuid();
            using Stream stream = await _resourceManager.GetResourceStreamAsync(TestDataFiles.TestDocument04);

            SectionReaderFactory readerFactory = new SectionReaderFactory(_ollamaEmbeddingService, _tokenizer);

            SectionReader sectionReader = readerFactory.CreateForMarkdown(stream, fileId, maxTokensPerChunk: 100, maxTokensPerSection: 1000);

            // Act
            List<List<KnowledgeFileChunk>> sections = new List<List<KnowledgeFileChunk>>();
            int sectionIndex = 0;
            await foreach (List<KnowledgeFileChunk> chunks in sectionReader.ReadSectionsAsync())
            {
                Console.WriteLine("=== SECTION START ===");
                Console.WriteLine(KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex).ToString("\n-------------\n"));
                int tokenCount = _tokenizer.CountTokens(KnowledgeFileSection.CreateFromChunks(chunks, fileId, sectionIndex).ToString());
                Console.WriteLine($"=== SECTION ENDED WITH {tokenCount} TOKENS ===");
                sectionIndex++;
            }
        }
    }
}
using EasyReasy.EnvironmentVariables;
using EasyReasy.FileStorage;
using EasyReasy.KnowledgeBase.Web.Server.Controllers;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;
using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Services.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Services.Hashing;
using EasyReasy.KnowledgeBase.Web.Server.Services.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Controllers
{
    /// <summary>
    /// Integration tests for the FileStorageController using real dependencies.
    /// </summary>
    [TestClass]
    public class FileStorageControllerIntegrationTests : DatabaseTestBase
    {
        private static ILibraryFileRepository _libraryFileRepository = null!;
        private static ILibraryRepository _libraryRepository = null!;
        private static ILibraryPermissionRepository _libraryPermissionRepository = null!;
        private static IUserRepository _userRepository = null!;
        private static ILibraryAuthorizationService _authorizationService = null!;
        private static IFileHashService _fileHashService = null!;
        private static IMemoryCache _memoryCache = null!;
        private static IFileSystem _fileSystem = null!;
        private static ILogger<FileStorageService> _fileStorageServiceLogger = null!;
        private static FileStorageService _fileStorageService = null!;
        private static FileStorageController _controller = null!;
        private static string _testFileStorageBasePath = null!;

        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testLibraryId = Guid.NewGuid();
        private User _testUser = null!;
        private Library _testLibrary = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseTestEnvironment();

            // Create repositories
            _libraryFileRepository = new LibraryFileRepository(_connectionFactory);
            _libraryRepository = new LibraryRepository(_connectionFactory);
            _libraryPermissionRepository = new LibraryPermissionRepository(_connectionFactory);
            _userRepository = new UserRepository(_connectionFactory);

            // Create authorization service
            using ILoggerFactory authLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger<LibraryAuthorizationService> authLogger = authLoggerFactory.CreateLogger<LibraryAuthorizationService>();
            _authorizationService = new LibraryAuthorizationService(_libraryRepository, _libraryPermissionRepository, authLogger);

            // Create file hash service
            _fileHashService = new Sha256FileHashService();

            // Create memory cache
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Create temporary directory for file storage
            _testFileStorageBasePath = Path.Combine(Path.GetTempPath(), "FileStorageControllerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testFileStorageBasePath);

            // Create file system
            _fileSystem = new LocalFileSystem(_testFileStorageBasePath);

            // Create logger for FileStorageService
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _fileStorageServiceLogger = loggerFactory.CreateLogger<FileStorageService>();

            // Create FileStorageService with real dependencies
            _fileStorageService = new FileStorageService(
                _fileSystem,
                _libraryFileRepository,
                _authorizationService,
                _fileHashService,
                _memoryCache,
                MaxFileSizeBytes,
                _fileStorageServiceLogger);

            // Create logger for controller
            ILogger<FileStorageController> controllerLogger = loggerFactory.CreateLogger<FileStorageController>();

            // Create controller
            _controller = new FileStorageController(_fileStorageService, controllerLogger);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Clean up test file storage directory
            if (Directory.Exists(_testFileStorageBasePath))
            {
                Directory.Delete(_testFileStorageBasePath, recursive: true);
            }

            // Dispose memory cache
            _memoryCache?.Dispose();
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            // Create test user
            _testUser = await _userRepository.CreateAsync(
                "testuser@example.com", "hashed_password", "Test", "User", new List<string> { "user" });

            // Create test library owned by test user
            _testLibrary = await _libraryRepository.CreateAsync(
                "Test Library", "A test library for integration tests", _testUser.Id, false);

            // Set up controller authentication context
            SetupAuthenticationContext(_testUser.Id);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up database tables (order matters due to foreign keys)
            string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            TestDatabaseMigrator.CleanupTable(connectionString, "library_file", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "library_permission", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "library", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);

            // Clean up any test files created during the test
            string libraryPath = Path.Combine(_testFileStorageBasePath, $"lib_{_testLibrary.Id:N}");
            if (Directory.Exists(libraryPath))
            {
                Directory.Delete(libraryPath, recursive: true);
            }

            // Clear memory cache
            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0); // Remove all cached items
            }
        }

        #region Chunked Upload Tests

        [TestMethod]
        public async Task InitiateChunkedUpload_WithValidRequest_ReturnsSuccessfulResponse()
        {
            // Arrange
            InitiateChunkedUploadRequest request = new InitiateChunkedUploadRequest(
                libraryId: _testLibrary.Id,
                fileName: "test-document.pdf",
                contentType: "application/pdf",
                totalSize: 2048,
                chunkSize: 1024);

            // Act
            IActionResult result = await _controller.InitiateChunkedUpload(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<ChunkedUploadResponse>(okResult.Value);

            ChunkedUploadResponse response = (ChunkedUploadResponse)okResult.Value!;
            Assert.AreNotEqual(Guid.Empty, response.SessionId);
            Assert.AreEqual(1024, response.ChunkSize);
            Assert.AreEqual(2, response.TotalChunks);
            Assert.AreEqual(0, response.UploadedChunks.Count);
            Assert.IsFalse(response.IsComplete);
            Assert.AreEqual(0.0, response.ProgressPercentage);
        }

        [TestMethod]
        public async Task InitiateChunkedUpload_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            IActionResult result = await _controller.InitiateChunkedUpload(null!, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
            BadRequestObjectResult badResult = (BadRequestObjectResult)result;
            Assert.AreEqual("Request body is required", badResult.Value);
        }

        [TestMethod]
        public async Task InitiateChunkedUpload_WithFileTooLarge_ReturnsBadRequest()
        {
            // Arrange
            InitiateChunkedUploadRequest request = new InitiateChunkedUploadRequest(
                libraryId: _testLibrary.Id,
                fileName: "huge-file.bin",
                contentType: "application/octet-stream",
                totalSize: MaxFileSizeBytes + 1, // Too large
                chunkSize: 1024);

            // Act
            IActionResult result = await _controller.InitiateChunkedUpload(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
            BadRequestObjectResult badResult = (BadRequestObjectResult)result;
            Assert.IsTrue(badResult.Value!.ToString()!.Contains("exceeds maximum allowed size"));
        }

        [TestMethod]
        public async Task UploadChunk_WithValidChunk_ReturnsSuccessfulResponse()
        {
            // Arrange
            ChunkedUploadSession session = await CreateTestUploadSession();
            byte[] chunkData = CreateTestChunkData(1024);

            using MemoryStream chunkStream = new MemoryStream(chunkData);
            Mock<IFormFile> mockFile = CreateMockFormFile(chunkData, "chunk0.dat");

            // Act
            IActionResult result = await _controller.UploadChunk(session.SessionId, 0, mockFile.Object, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<ChunkedUploadResponse>(okResult.Value);

            ChunkedUploadResponse response = (ChunkedUploadResponse)okResult.Value!;
            Assert.IsTrue(response.UploadedChunks.Contains(0));
            Assert.AreEqual(1, response.UploadedChunks.Count);
        }

        [TestMethod]
        public async Task UploadChunk_WithNullChunkFile_ReturnsBadRequest()
        {
            // Arrange
            ChunkedUploadSession session = await CreateTestUploadSession();

            // Act
            IActionResult result = await _controller.UploadChunk(session.SessionId, 0, null!, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
            BadRequestObjectResult badResult = (BadRequestObjectResult)result;
            Assert.AreEqual("Chunk file is required and cannot be empty", badResult.Value);
        }

        [TestMethod]
        public async Task UploadChunk_WithNegativeChunkNumber_ReturnsBadRequest()
        {
            // Arrange
            ChunkedUploadSession session = await CreateTestUploadSession();
            byte[] chunkData = CreateTestChunkData(1024);
            Mock<IFormFile> mockFile = CreateMockFormFile(chunkData, "chunk.dat");

            // Act
            IActionResult result = await _controller.UploadChunk(session.SessionId, -1, mockFile.Object, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
            BadRequestObjectResult badResult = (BadRequestObjectResult)result;
            Assert.AreEqual("Chunk number must be non-negative", badResult.Value);
        }

        [TestMethod]
        public async Task CompleteChunkedUpload_WithCompleteSession_ReturnsSuccessfulResponse()
        {
            // Arrange
            ChunkedUploadSession session = await CreateCompleteTestUploadSession();

            // Act
            IActionResult result = await _controller.CompleteChunkedUpload(session.SessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<FileUploadResponse>(okResult.Value);

            FileUploadResponse response = (FileUploadResponse)okResult.Value!;
            Assert.IsTrue(response.Success);
            Assert.AreEqual("File uploaded successfully", response.Message);
            Assert.IsNotNull(response.FileInfo);
            Assert.AreEqual("test-document.pdf", response.FileInfo.OriginalFileName);
        }

        [TestMethod]
        public async Task CompleteChunkedUpload_WithIncompleteSession_ReturnsBadRequest()
        {
            // Arrange - Create session but don't upload all chunks
            ChunkedUploadSession session = await CreateTestUploadSession();
            // Upload only first chunk, leaving second chunk missing
            await UploadTestChunk(session.SessionId, 0, 1024);

            // Act
            IActionResult result = await _controller.CompleteChunkedUpload(session.SessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
            BadRequestObjectResult badResult = (BadRequestObjectResult)result;
            Assert.IsTrue(badResult.Value!.ToString()!.Contains("is not complete"));
        }

        [TestMethod]
        public async Task GetUploadStatus_WithValidSession_ReturnsSessionStatus()
        {
            // Arrange
            ChunkedUploadSession session = await CreateTestUploadSession();
            await UploadTestChunk(session.SessionId, 0, 1024);

            // Act
            IActionResult result = await _controller.GetUploadStatus(session.SessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<ChunkedUploadResponse>(okResult.Value);

            ChunkedUploadResponse response = (ChunkedUploadResponse)okResult.Value!;
            Assert.AreEqual(session.SessionId, response.SessionId);
            Assert.IsTrue(response.UploadedChunks.Contains(0));
            Assert.IsFalse(response.IsComplete);
        }

        [TestMethod]
        public async Task GetUploadStatus_WithNonExistentSession_ReturnsNotFound()
        {
            // Arrange
            Guid nonExistentSessionId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.GetUploadStatus(nonExistentSessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
            NotFoundObjectResult notFoundResult = (NotFoundObjectResult)result;
            Assert.AreEqual("Upload session not found or expired", notFoundResult.Value);
        }

        [TestMethod]
        public async Task CancelChunkedUpload_WithValidSession_ReturnsSuccessfulResponse()
        {
            // Arrange
            ChunkedUploadSession session = await CreateTestUploadSession();

            // Act
            IActionResult result = await _controller.CancelChunkedUpload(session.SessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;

            // Verify session is removed
            IActionResult statusResult = await _controller.GetUploadStatus(session.SessionId, CancellationToken.None);
            Assert.IsInstanceOfType<NotFoundObjectResult>(statusResult);
        }

        [TestMethod]
        public async Task CancelChunkedUpload_WithNonExistentSession_ReturnsNotFound()
        {
            // Arrange
            Guid nonExistentSessionId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.CancelChunkedUpload(nonExistentSessionId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion

        #region File Management Tests

        [TestMethod]
        public async Task ListFiles_WithValidLibrary_ReturnsFileList()
        {
            // Arrange
            LibraryFile testFile = await CreateTestFile("document1.pdf", "application/pdf", 1024);
            LibraryFile testFile2 = await CreateTestFile("document2.txt", "text/plain", 512);

            // Act
            IActionResult result = await _controller.ListFiles(_testLibrary.Id, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<IEnumerable<LibraryFileDto>>(okResult.Value);

            IEnumerable<LibraryFileDto> files = (IEnumerable<LibraryFileDto>)okResult.Value!;
            List<LibraryFileDto> fileList = files.ToList();
            Assert.AreEqual(2, fileList.Count);

            // Verify both files are present
            Assert.IsTrue(fileList.Any(f => f.FileId == testFile.Id && f.OriginalFileName == "document1.pdf"));
            Assert.IsTrue(fileList.Any(f => f.FileId == testFile2.Id && f.OriginalFileName == "document2.txt"));
        }

        [TestMethod]
        public async Task ListFiles_WithEmptyLibrary_ReturnsEmptyList()
        {
            // Act
            IActionResult result = await _controller.ListFiles(_testLibrary.Id, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<IEnumerable<LibraryFileDto>>(okResult.Value);

            IEnumerable<LibraryFileDto> files = (IEnumerable<LibraryFileDto>)okResult.Value!;
            Assert.AreEqual(0, files.Count());
        }

        [TestMethod]
        public async Task GetFileInfo_WithValidFile_ReturnsFileInfo()
        {
            // Arrange
            LibraryFile testFile = await CreateTestFile("test-document.pdf", "application/pdf", 2048);

            // Act
            IActionResult result = await _controller.GetFileInfo(_testLibrary.Id, testFile.Id, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
            OkObjectResult okResult = (OkObjectResult)result;
            Assert.IsInstanceOfType<LibraryFileDto>(okResult.Value);

            LibraryFileDto fileInfo = (LibraryFileDto)okResult.Value!;
            Assert.AreEqual(testFile.Id, fileInfo.FileId);
            Assert.AreEqual("test-document.pdf", fileInfo.OriginalFileName);
            Assert.AreEqual("application/pdf", fileInfo.ContentType);
            Assert.AreEqual(2048, fileInfo.SizeInBytes);
        }

        [TestMethod]
        public async Task GetFileInfo_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            Guid nonExistentFileId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.GetFileInfo(_testLibrary.Id, nonExistentFileId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
            NotFoundObjectResult notFoundResult = (NotFoundObjectResult)result;
            Assert.AreEqual("File not found", notFoundResult.Value);
        }

        [TestMethod]
        public async Task DownloadFile_WithValidFile_ReturnsFileStream()
        {
            // Arrange
            string testContent = "This is a test file content for download testing.";
            LibraryFile testFile = await CreateTestFileWithContent("download-test.txt", "text/plain", testContent);

            // Act
            IActionResult result = await _controller.DownloadFile(_testLibrary.Id, testFile.Id, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<FileStreamResult>(result);
            FileStreamResult fileResult = (FileStreamResult)result;
            Assert.AreEqual("text/plain", fileResult.ContentType);
            Assert.AreEqual("download-test.txt", fileResult.FileDownloadName);

            // Verify file content
            using StreamReader reader = new StreamReader(fileResult.FileStream);
            string downloadedContent = await reader.ReadToEndAsync();
            Assert.AreEqual(testContent, downloadedContent);
        }

        [TestMethod]
        public async Task DownloadFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            Guid nonExistentFileId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.DownloadFile(_testLibrary.Id, nonExistentFileId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
            NotFoundObjectResult notFoundResult = (NotFoundObjectResult)result;
            Assert.AreEqual("File not found", notFoundResult.Value);
        }

        [TestMethod]
        public async Task DeleteFile_WithValidFile_DeletesFileSuccessfully()
        {
            // Arrange
            LibraryFile testFile = await CreateTestFileWithContent("delete-test.txt", "text/plain", "Test content");

            // Act
            IActionResult result = await _controller.DeleteFile(_testLibrary.Id, testFile.Id, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);

            // Verify file is deleted
            IActionResult getResult = await _controller.GetFileInfo(_testLibrary.Id, testFile.Id, CancellationToken.None);
            Assert.IsInstanceOfType<NotFoundObjectResult>(getResult);
        }

        [TestMethod]
        public async Task DeleteFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            Guid nonExistentFileId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.DeleteFile(_testLibrary.Id, nonExistentFileId, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Authentication Tests

        [TestMethod]
        public async Task InitiateChunkedUpload_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            SetupAuthenticationContext(null); // Clear authentication
            InitiateChunkedUploadRequest request = new InitiateChunkedUploadRequest(
                libraryId: _testLibrary.Id,
                fileName: "test.txt",
                contentType: "text/plain",
                totalSize: 1024,
                chunkSize: 512);

            // Act
            IActionResult result = await _controller.InitiateChunkedUpload(request, CancellationToken.None);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result);
            UnauthorizedObjectResult unauthorizedResult = (UnauthorizedObjectResult)result;
            Assert.AreEqual("Invalid user authentication", unauthorizedResult.Value);
        }

        #endregion

        #region Helper Methods

        private void SetupAuthenticationContext(Guid? userId)
        {
            Mock<HttpContext> mockHttpContext = new Mock<HttpContext>();

            if (userId.HasValue)
            {
                // Create claims identity with user ID - use standard JWT claim types
                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()), // Standard user ID claim
                    new Claim("sub", userId.Value.ToString()), // JWT subject claim
                    new Claim(ClaimTypes.Email, _testUser?.Email ?? "test@example.com"),
                    new Claim(ClaimTypes.GivenName, _testUser?.FirstName ?? "Test"),
                    new Claim(ClaimTypes.Surname, _testUser?.LastName ?? "User"),
                    new Claim("email", _testUser?.Email ?? "test@example.com"),
                    new Claim("first_name", _testUser?.FirstName ?? "Test"),
                    new Claim("last_name", _testUser?.LastName ?? "User")
                };

                // Create authenticated identity
                ClaimsIdentity identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.NameIdentifier, ClaimTypes.Role);
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                // Set up Items dictionary for EasyReasy.Auth extension methods
                Dictionary<object, object?> items = new Dictionary<object, object?>
                {
                    ["UserId"] = userId.Value.ToString(),
                    ["TenantId"] = "test-tenant"
                };

                mockHttpContext.Setup(c => c.User).Returns(principal);
                mockHttpContext.Setup(c => c.Items).Returns(items);
            }
            else
            {
                // Set up context without authentication
                mockHttpContext.Setup(c => c.User).Returns(new ClaimsPrincipal());
                mockHttpContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());
            }

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
        }

        private async Task<ChunkedUploadSession> CreateTestUploadSession()
        {
            return await _fileStorageService.InitiateChunkedUploadAsync(
                libraryId: _testLibrary.Id,
                fileName: "test-document.pdf",
                contentType: "application/pdf",
                totalSize: 2048,
                chunkSize: 1024,
                uploadedByUserId: _testUser.Id);
        }

        private async Task<ChunkedUploadSession> CreateCompleteTestUploadSession()
        {
            ChunkedUploadSession session = await CreateTestUploadSession();

            // Upload both chunks to make it complete
            await UploadTestChunk(session.SessionId, 0, 1024);
            await UploadTestChunk(session.SessionId, 1, 1024);

            // Get the updated session
            ChunkedUploadSession? updatedSession = await _fileStorageService.GetChunkedUploadSessionAsync(session.SessionId);
            Assert.IsNotNull(updatedSession);
            Assert.IsTrue(updatedSession.IsComplete);

            return updatedSession;
        }

        private async Task UploadTestChunk(Guid sessionId, int chunkNumber, int chunkSize)
        {
            byte[] chunkData = CreateTestChunkData(chunkSize);
            using MemoryStream chunkStream = new MemoryStream(chunkData);
            await _fileStorageService.UploadChunkAsync(sessionId, chunkNumber, chunkStream);
        }

        private static byte[] CreateTestChunkData(int size)
        {
            byte[] data = new byte[size];
            Random random = new Random();
            random.NextBytes(data);
            return data;
        }

        private static Mock<IFormFile> CreateMockFormFile(byte[] content, string fileName)
        {
            Mock<IFormFile> mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
            return mockFile;
        }

        private async Task<LibraryFile> CreateTestFile(string fileName, string contentType, long sizeInBytes)
        {
            byte[] testHash = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            string relativePath = Path.Combine($"lib_{_testLibrary.Id:N}", fileName);

            return await _libraryFileRepository.CreateAsync(
                libraryId: _testLibrary.Id,
                originalFileName: fileName,
                contentType: contentType,
                sizeInBytes: sizeInBytes,
                relativePath: relativePath,
                hash: testHash,
                uploadedByUserId: _testUser.Id);
        }

        private async Task<LibraryFile> CreateTestFileWithContent(string fileName, string contentType, string content)
        {
            // Create the file on disk
            string libraryPath = Path.Combine(_testFileStorageBasePath, $"lib_{_testLibrary.Id:N}");
            Directory.CreateDirectory(libraryPath);
            string filePath = Path.Combine(libraryPath, fileName);
            await File.WriteAllTextAsync(filePath, content);

            // Compute hash
            byte[] hash = await _fileHashService.ComputeHashAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

            // Create database record with relative path (relative to the FileSystem base path)
            string relativePath = Path.Combine($"lib_{_testLibrary.Id:N}", fileName);
            return await _libraryFileRepository.CreateAsync(
                libraryId: _testLibrary.Id,
                originalFileName: fileName,
                contentType: contentType,
                sizeInBytes: content.Length,
                relativePath: relativePath,
                hash: hash,
                uploadedByUserId: _testUser.Id);
        }

        #endregion
    }
}

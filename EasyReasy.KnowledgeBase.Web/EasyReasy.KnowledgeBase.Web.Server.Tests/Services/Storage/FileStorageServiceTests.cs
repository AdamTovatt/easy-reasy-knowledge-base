using EasyReasy.FileStorage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;
using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Services.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Services.Hashing;
using EasyReasy.KnowledgeBase.Web.Server.Services.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Services.Storage
{
    /// <summary>
    /// Unit tests for the FileStorageService using a hybrid approach:
    /// - Real MemoryCache for realistic session management
    /// - Mocked dependencies for controlled behavior
    /// </summary>
    [TestClass]
    public class FileStorageServiceTests
    {
        private Mock<IFileSystem> _mockFileSystem = null!;
        private Mock<ILibraryFileRepository> _mockFileRepository = null!;
        private Mock<ILibraryAuthorizationService> _mockAuthorizationService = null!;
        private Mock<IFileHashService> _mockFileHashService = null!;
        private IMemoryCache _memoryCache = null!; // Real MemoryCache
        private Mock<ILogger<FileStorageService>> _mockLogger = null!;
        private FileStorageService _fileStorageService = null!;

        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB
        private readonly Guid _testLibraryId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testFileId = Guid.NewGuid();

        [TestInitialize]
        public void TestInitialize()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileRepository = new Mock<ILibraryFileRepository>();
            _mockAuthorizationService = new Mock<ILibraryAuthorizationService>();
            _mockFileHashService = new Mock<IFileHashService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<FileStorageService>>();

            _fileStorageService = new FileStorageService(
                _mockFileSystem.Object,
                _mockFileRepository.Object,
                _mockAuthorizationService.Object,
                _mockFileHashService.Object,
                _memoryCache,
                MaxFileSizeBytes,
                _mockLogger.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _memoryCache?.Dispose();
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(null!, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, _memoryCache, MaxFileSizeBytes, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullFileRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(_mockFileSystem.Object, null!, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, _memoryCache, MaxFileSizeBytes, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullAuthorizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, null!,
                    _mockFileHashService.Object, _memoryCache, MaxFileSizeBytes, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullFileHashService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    null!, _memoryCache, MaxFileSizeBytes, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullSessionCache_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, null!, MaxFileSizeBytes, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, _memoryCache, MaxFileSizeBytes, null!));
        }

        [TestMethod]
        public void Constructor_WithZeroMaxFileSize_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = Assert.ThrowsException<ArgumentException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, _memoryCache, 0, _mockLogger.Object));

            Assert.IsTrue(exception.Message.Contains("Max file size must be greater than zero"));
        }

        [TestMethod]
        public void Constructor_WithNegativeMaxFileSize_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = Assert.ThrowsException<ArgumentException>(() =>
                new FileStorageService(_mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                    _mockFileHashService.Object, _memoryCache, -1, _mockLogger.Object));

            Assert.IsTrue(exception.Message.Contains("Max file size must be greater than zero"));
        }

        [TestMethod]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            FileStorageService service = new FileStorageService(
                _mockFileSystem.Object, _mockFileRepository.Object, _mockAuthorizationService.Object,
                _mockFileHashService.Object, _memoryCache, MaxFileSizeBytes, _mockLogger.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region InitiateChunkedUploadAsync Tests

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithValidData_CreatesSession()
        {
            // Arrange
            string fileName = "test.txt";
            string contentType = "text/plain";
            long totalSize = 1024;
            int chunkSize = 512;

            SetupValidUploadPermissions();
            SetupFileSystemForSessionCreation();

            // Act
            ChunkedUploadSession result = await _fileStorageService.InitiateChunkedUploadAsync(
                _testLibraryId, fileName, contentType, totalSize, chunkSize, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(Guid.Empty, result.SessionId);
            Assert.AreEqual(_testLibraryId, result.LibraryId);
            Assert.AreEqual(fileName, result.OriginalFileName);
            Assert.AreEqual(contentType, result.ContentType);
            Assert.AreEqual(totalSize, result.TotalSize);
            Assert.AreEqual(chunkSize, result.ChunkSize);
            Assert.AreEqual(_testUserId, result.UploadedByUserId);
            Assert.AreEqual(2, result.TotalChunks); // 1024 / 512 = 2
            Assert.IsFalse(result.IsComplete);
            Assert.IsFalse(result.IsExpired);
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithNullFileName_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, null!, "text/plain", 1024, 512, _testUserId));

            Assert.IsTrue(exception.Message.Contains("File name cannot be null or empty"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithFileTooLarge_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "text/plain", MaxFileSizeBytes + 1, 512, _testUserId));

            Assert.IsTrue(exception.Message.Contains("exceeds maximum allowed size"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithoutWritePermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Write, "initiate file upload"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "text/plain", 1024, 512, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithNullContentType_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", null!, 1024, 512, _testUserId));

            Assert.IsTrue(exception.Message.Contains("Content type cannot be null or empty"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithEmptyContentType_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "", 1024, 512, _testUserId));

            Assert.IsTrue(exception.Message.Contains("Content type cannot be null or empty"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithZeroChunkSize_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "text/plain", 1024, 0, _testUserId));

            Assert.IsTrue(exception.Message.Contains("Chunk size must be between 1 and 50MB"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithNegativeChunkSize_ThrowsArgumentException()
        {
            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "text/plain", 1024, -1, _testUserId));

            Assert.IsTrue(exception.Message.Contains("Chunk size must be between 1 and 50MB"));
        }

        [TestMethod]
        public async Task InitiateChunkedUploadAsync_WithTooLargeChunkSize_ThrowsArgumentException()
        {
            // Arrange
            int tooLargeChunkSize = 51 * 1024 * 1024; // 51MB
            long totalSize = 60 * 1024 * 1024; // 60MB (less than MaxFileSizeBytes)

            // Act & Assert
            ArgumentException exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _fileStorageService.InitiateChunkedUploadAsync(_testLibraryId, "test.txt", "text/plain", totalSize, tooLargeChunkSize, _testUserId));

            Assert.IsTrue(exception.Message.Contains("Chunk size must be between 1 and 50MB"));
        }

        #endregion

        #region UploadChunkAsync Tests

        [TestMethod]
        public async Task UploadChunkAsync_WithValidChunk_UpdatesSession()
        {
            // Arrange
            ChunkedUploadSession session = await CreateValidUploadSession();
            SetupFileSystemForChunkUpload();

            byte[] chunkData = new byte[512];
            using MemoryStream chunkStream = new MemoryStream(chunkData);

            // Act
            ChunkedUploadSession result = await _fileStorageService.UploadChunkAsync(session.SessionId, 0, chunkStream);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(session.SessionId, result.SessionId);
            Assert.IsTrue(result.UploadedChunks.Contains(0));
            Assert.AreEqual(1, result.UploadedChunks.Count);
        }

        [TestMethod]
        public async Task UploadChunkAsync_WithNullChunkData_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _fileStorageService.UploadChunkAsync(Guid.NewGuid(), 0, null!));

            Assert.AreEqual("chunkData", exception.ParamName);
        }

        [TestMethod]
        public async Task UploadChunkAsync_WithNonExistentSession_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid nonExistentSessionId = Guid.NewGuid();

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _fileStorageService.UploadChunkAsync(nonExistentSessionId, 0, new MemoryStream()));

            Assert.IsTrue(exception.Message.Contains("not found or expired"));
        }

        [TestMethod]
        public async Task UploadChunkAsync_WithDuplicateChunk_ThrowsInvalidOperationException()
        {
            // Arrange
            ChunkedUploadSession session = await CreateValidUploadSession();
            SetupFileSystemForChunkUpload();

            byte[] chunkData = new byte[512];
            using MemoryStream chunkStream1 = new MemoryStream(chunkData);
            using MemoryStream chunkStream2 = new MemoryStream(chunkData);

            // Upload chunk 0 first
            await _fileStorageService.UploadChunkAsync(session.SessionId, 0, chunkStream1);

            // Act & Assert - Try to upload chunk 0 again
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _fileStorageService.UploadChunkAsync(session.SessionId, 0, chunkStream2));

            Assert.IsTrue(exception.Message.Contains("already been uploaded"));
        }

        #endregion

        #region CompleteChunkedUploadAsync Tests

        [TestMethod]
        public async Task CompleteChunkedUploadAsync_WithCompleteSession_CreatesFile()
        {
            // Arrange
            ChunkedUploadSession session = await CreateCompleteUploadSession();
            SetupFileSystemForCompletion();
            SetupFileHashService();
            SetupFileRepositoryForCreation();

            // Act
            LibraryFileDto result = await _fileStorageService.CompleteChunkedUploadAsync(session.SessionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testFileId, result.FileId);
            Assert.AreEqual(session.OriginalFileName, result.OriginalFileName);
            Assert.AreEqual(session.ContentType, result.ContentType);

            // Verify file was saved to repository
            _mockFileRepository.Verify(r => r.CreateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public async Task CompleteChunkedUploadAsync_WithNonExistentSession_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid nonExistentSessionId = Guid.NewGuid();

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _fileStorageService.CompleteChunkedUploadAsync(nonExistentSessionId));

            Assert.IsTrue(exception.Message.Contains("not found or expired"));
        }

        [TestMethod]
        public async Task CompleteChunkedUploadAsync_WithIncompleteSession_ThrowsInvalidOperationException()
        {
            // Arrange
            ChunkedUploadSession session = await CreateValidUploadSession(); // Not complete

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _fileStorageService.CompleteChunkedUploadAsync(session.SessionId));

            Assert.IsTrue(exception.Message.Contains("is not complete"));
        }

        [TestMethod]
        public async Task CompleteChunkedUploadAsync_WithFileSizeMismatch_ThrowsInvalidOperationException()
        {
            // Arrange
            ChunkedUploadSession session = await CreateCompleteUploadSession();
            SetupFileHashService();

            // Setup file system to return wrong file size
            byte[] testFileContent = new byte[512]; // Should be 1024
            MemoryStream tempStream = new MemoryStream(testFileContent);
            MemoryStream finalStream = new MemoryStream();

            _mockFileSystem
                .Setup(fs => fs.OpenFileForReadingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempStream);

            _mockFileSystem
                .Setup(fs => fs.OpenFileForWritingAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(finalStream);

            // Return wrong file size
            _mockFileSystem
                .Setup(fs => fs.GetFileSizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(512); // Should be 1024

            _mockFileSystem
                .Setup(fs => fs.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _fileStorageService.CompleteChunkedUploadAsync(session.SessionId));

            Assert.IsTrue(exception.Message.Contains("File size mismatch"));
        }

        [TestMethod]
        public async Task CompleteChunkedUploadAsync_WithFileSystemFailure_ThrowsException()
        {
            // Arrange
            ChunkedUploadSession session = await CreateCompleteUploadSession();

            _mockFileSystem
                .Setup(fs => fs.OpenFileForReadingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("File system error"));

            // Act & Assert
            IOException exception = await Assert.ThrowsExceptionAsync<IOException>(
                () => _fileStorageService.CompleteChunkedUploadAsync(session.SessionId));

            Assert.AreEqual("File system error", exception.Message);
        }

        #endregion

        #region CancelChunkedUploadAsync Tests

        [TestMethod]
        public async Task CancelChunkedUploadAsync_WithExistingSession_CancelsAndCleansUp()
        {
            // Arrange
            ChunkedUploadSession session = await CreateValidUploadSession();
            SetupFileSystemForCleanup();

            // Act
            bool result = await _fileStorageService.CancelChunkedUploadAsync(session.SessionId);

            // Assert
            Assert.IsTrue(result);

            // Verify session is no longer in cache
            ChunkedUploadSession? retrievedSession = await _fileStorageService.GetChunkedUploadSessionAsync(session.SessionId);
            Assert.IsNull(retrievedSession);
        }

        [TestMethod]
        public async Task CancelChunkedUploadAsync_WithNonExistentSession_ReturnsFalse()
        {
            // Arrange
            Guid nonExistentSessionId = Guid.NewGuid();

            // Act
            bool result = await _fileStorageService.CancelChunkedUploadAsync(nonExistentSessionId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Session Expiration Tests

        [TestMethod]
        public async Task GetChunkedUploadSessionAsync_WithExpiredSession_ReturnsNull()
        {
            // Arrange
            SetupValidUploadPermissions();
            SetupFileSystemForSessionCreation();
            SetupFileSystemForCleanup();

            // Create session
            ChunkedUploadSession session = await _fileStorageService.InitiateChunkedUploadAsync(
                _testLibraryId, "test.txt", "text/plain", 1024, 512, _testUserId);

            // Manually create an expired session and put it in cache
            ChunkedUploadSession expiredSession = new ChunkedUploadSession(
                sessionId: session.SessionId,
                libraryId: session.LibraryId,
                originalFileName: session.OriginalFileName,
                contentType: session.ContentType,
                totalSize: session.TotalSize,
                chunkSize: session.ChunkSize,
                uploadedByUserId: session.UploadedByUserId,
                createdAt: DateTime.UtcNow.AddDays(-2), // Created 2 days ago
                expiresAt: DateTime.UtcNow.AddDays(-1)  // Expired 1 day ago
            );
            expiredSession.TempFilePath = session.TempFilePath;

            // Replace session in cache with expired one
            string cacheKey = $"chunked_upload_session_{session.SessionId:N}";
            _memoryCache.Remove(cacheKey);
            _memoryCache.Set(cacheKey, expiredSession, expiredSession.ExpiresAt);

            // Act
            ChunkedUploadSession? result = await _fileStorageService.GetChunkedUploadSessionAsync(session.SessionId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetChunkedUploadSessionAsync_WithValidSession_ReturnsSession()
        {
            // Arrange
            ChunkedUploadSession session = await CreateValidUploadSession();

            // Act
            ChunkedUploadSession? result = await _fileStorageService.GetChunkedUploadSessionAsync(session.SessionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(session.SessionId, result.SessionId);
            Assert.IsFalse(result.IsExpired);
        }

        #endregion

        #region GetFileInfoAsync Tests

        [TestMethod]
        public async Task GetFileInfoAsync_WithValidFile_ReturnsFileInfo()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file information"))
                .Returns(Task.CompletedTask);

            LibraryFile mockFile = CreateTestLibraryFile();
            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(mockFile);

            // Act
            LibraryFileDto? result = await _fileStorageService.GetFileInfoAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(mockFile.Id, result.FileId);
            Assert.AreEqual(mockFile.OriginalFileName, result.OriginalFileName);
        }

        [TestMethod]
        public async Task GetFileInfoAsync_WithoutReadPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file information"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.GetFileInfoAsync(_testLibraryId, _testFileId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        [TestMethod]
        public async Task GetFileInfoAsync_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file information"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync((LibraryFile?)null);

            // Act
            LibraryFileDto? result = await _fileStorageService.GetFileInfoAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetFileStreamAsync Tests

        [TestMethod]
        public async Task GetFileStreamAsync_WithValidFile_ReturnsStream()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file content"))
                .Returns(Task.CompletedTask);

            LibraryFile mockFile = CreateTestLibraryFile();
            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(mockFile);

            Mock<Stream> mockStream = new Mock<Stream>();
            _mockFileSystem
                .Setup(fs => fs.OpenFileForReadingAsync(mockFile.RelativePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockStream.Object);

            // Act
            Stream result = await _fileStorageService.GetFileStreamAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(mockStream.Object, result);
        }

        #endregion

        #region DeleteFileAsync Tests

        [TestMethod]
        public async Task DeleteFileAsync_WithValidFile_DeletesFile()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Write, "delete file"))
                .Returns(Task.CompletedTask);

            LibraryFile mockFile = CreateTestLibraryFile();
            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(mockFile);

            _mockFileRepository
                .Setup(r => r.DeleteAsync(_testFileId))
                .ReturnsAsync(true);

            _mockFileSystem
                .Setup(fs => fs.FileExistsAsync(mockFile.RelativePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileSystem
                .Setup(fs => fs.DeleteFileAsync(mockFile.RelativePath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            bool result = await _fileStorageService.DeleteFileAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsTrue(result);
            _mockFileRepository.Verify(r => r.DeleteAsync(_testFileId), Times.Once);
            _mockFileSystem.Verify(fs => fs.DeleteFileAsync(mockFile.RelativePath, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteFileAsync_WithoutWritePermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Write, "delete file"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.DeleteFileAsync(_testLibraryId, _testFileId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        #endregion

        #region GetFileContentAsync Tests

        [TestMethod]
        public async Task GetFileContentAsync_WithValidFile_ReturnsContent()
        {
            // Arrange
            string expectedContent = "Test file content";

            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file content"))
                .Returns(Task.CompletedTask);

            LibraryFile mockFile = CreateTestLibraryFile();
            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(mockFile);

            _mockFileSystem
                .Setup(fs => fs.ReadFileAsTextAsync(mockFile.RelativePath, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedContent);

            // Act
            string result = await _fileStorageService.GetFileContentAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.AreEqual(expectedContent, result);
        }

        [TestMethod]
        public async Task GetFileContentAsync_WithoutReadPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file content"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.GetFileContentAsync(_testLibraryId, _testFileId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        [TestMethod]
        public async Task GetFileContentAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "access file content"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.GetByIdInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync((LibraryFile?)null);

            // Act & Assert
            FileNotFoundException exception = await Assert.ThrowsExceptionAsync<FileNotFoundException>(
                () => _fileStorageService.GetFileContentAsync(_testLibraryId, _testFileId, _testUserId));

            Assert.IsTrue(exception.Message.Contains("not found"));
        }

        #endregion

        #region ListFilesAsync Tests

        [TestMethod]
        public async Task ListFilesAsync_WithValidLibrary_ReturnsFileList()
        {
            // Arrange
            List<LibraryFile> mockFiles = new List<LibraryFile>
            {
                CreateTestLibraryFile(),
                CreateTestLibraryFile() // Will have same ID but that's fine for this test
            };

            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "list files"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.GetByKnowledgeBaseIdAsync(_testLibraryId))
                .ReturnsAsync(mockFiles);

            // Act
            IEnumerable<LibraryFileDto> result = await _fileStorageService.ListFilesAsync(_testLibraryId, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task ListFilesAsync_WithoutReadPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "list files"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.ListFilesAsync(_testLibraryId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        #endregion

        #region FileExistsAsync Tests

        [TestMethod]
        public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "check file existence"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.ExistsInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(true);

            // Act
            bool result = await _fileStorageService.FileExistsAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "check file existence"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.ExistsInKnowledgeBaseAsync(_testLibraryId, _testFileId))
                .ReturnsAsync(false);

            // Act
            bool result = await _fileStorageService.FileExistsAsync(_testLibraryId, _testFileId, _testUserId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task FileExistsAsync_WithoutReadPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Read, "check file existence"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.FileExistsAsync(_testLibraryId, _testFileId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        #endregion

        #region EnsureLibraryExistsAsync Tests

        [TestMethod]
        public async Task EnsureLibraryExistsAsync_WithNonExistentDirectory_CreatesDirectory()
        {
            // Arrange
            _mockFileSystem
                .Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockFileSystem
                .Setup(fs => fs.CreateDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _fileStorageService.EnsureLibraryExistsAsync(_testLibraryId);

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task EnsureLibraryExistsAsync_WithExistingDirectory_DoesNotCreateDirectory()
        {
            // Arrange
            _mockFileSystem
                .Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _fileStorageService.EnsureLibraryExistsAsync(_testLibraryId);

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region DeleteLibraryAsync Tests

        [TestMethod]
        public async Task DeleteLibraryAsync_WithExistingLibrary_DeletesLibraryAndFiles()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Admin, "delete library"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.DeleteByKnowledgeBaseIdAsync(_testLibraryId))
                .ReturnsAsync(5); // 5 files deleted

            _mockFileSystem
                .Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileSystem
                .Setup(fs => fs.DeleteDirectoryAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            bool result = await _fileStorageService.DeleteLibraryAsync(_testLibraryId, _testUserId);

            // Assert
            Assert.IsTrue(result);
            _mockFileRepository.Verify(r => r.DeleteByKnowledgeBaseIdAsync(_testLibraryId), Times.Once);
            _mockFileSystem.Verify(fs => fs.DeleteDirectoryAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteLibraryAsync_WithoutAdminPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Admin, "delete library"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _fileStorageService.DeleteLibraryAsync(_testLibraryId, _testUserId));

            Assert.AreEqual("Access denied", exception.Message);
        }

        [TestMethod]
        public async Task DeleteLibraryAsync_WithEmptyLibrary_ReturnsFalse()
        {
            // Arrange
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Admin, "delete library"))
                .Returns(Task.CompletedTask);

            _mockFileRepository
                .Setup(r => r.DeleteByKnowledgeBaseIdAsync(_testLibraryId))
                .ReturnsAsync(0); // No files deleted

            _mockFileSystem
                .Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            bool result = await _fileStorageService.DeleteLibraryAsync(_testLibraryId, _testUserId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Helper Methods

        private void SetupValidUploadPermissions()
        {
            _mockAuthorizationService
                .Setup(s => s.ValidateAccessAsync(_testUserId, _testLibraryId, LibraryPermissionType.Write, "initiate file upload"))
                .Returns(Task.CompletedTask);
        }

        private void SetupFileSystemForSessionCreation()
        {
            _mockFileSystem
                .Setup(fs => fs.CreateDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupFileSystemForChunkUpload()
        {
            // Use a real MemoryStream instead of mocking Stream since CopyToAsync is not virtual
            MemoryStream memoryStream = new MemoryStream();

            _mockFileSystem
                .Setup(fs => fs.OpenFileForWritingAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryStream);
        }

        private void SetupFileSystemForCompletion()
        {
            // Use real MemoryStreams since CopyToAsync is not virtual
            byte[] testFileContent = new byte[1024]; // 1KB test content
            MemoryStream tempStream = new MemoryStream(testFileContent);
            MemoryStream finalStream = new MemoryStream();

            _mockFileSystem
                .Setup(fs => fs.OpenFileForReadingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempStream);

            _mockFileSystem
                .Setup(fs => fs.OpenFileForWritingAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(finalStream);

            _mockFileSystem
                .Setup(fs => fs.GetFileSizeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1024);
        }

        private void SetupFileSystemForCleanup()
        {
            _mockFileSystem
                .Setup(fs => fs.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupFileHashService()
        {
            byte[] testHash = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            _mockFileHashService
                .Setup(h => h.ComputeHashAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testHash);
        }

        private void SetupFileRepositoryForCreation()
        {
            LibraryFile mockFile = CreateTestLibraryFile();
            _mockFileRepository
                .Setup(r => r.CreateAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<long>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockFile);
        }

        private async Task<ChunkedUploadSession> CreateValidUploadSession()
        {
            SetupValidUploadPermissions();
            SetupFileSystemForSessionCreation();

            return await _fileStorageService.InitiateChunkedUploadAsync(
                _testLibraryId, "test.txt", "text/plain", 1024, 512, _testUserId);
        }

        private async Task<ChunkedUploadSession> CreateCompleteUploadSession()
        {
            ChunkedUploadSession session = await CreateValidUploadSession();

            // Create a fresh MemoryStream for each call to avoid disposal issues
            _mockFileSystem
                .Setup(fs => fs.OpenFileForWritingAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .Returns<string, bool, CancellationToken>((path, append, ct) =>
                    Task.FromResult<Stream>(new MemoryStream()));

            // Upload both chunks to make it complete
            byte[] chunkData = new byte[512];
            using (MemoryStream chunkStream1 = new MemoryStream(chunkData))
            {
                await _fileStorageService.UploadChunkAsync(session.SessionId, 0, chunkStream1);
            }

            using (MemoryStream chunkStream2 = new MemoryStream(chunkData))
            {
                await _fileStorageService.UploadChunkAsync(session.SessionId, 1, chunkStream2);
            }

            // Get the updated session
            ChunkedUploadSession? updatedSession = await _fileStorageService.GetChunkedUploadSessionAsync(session.SessionId);
            Assert.IsNotNull(updatedSession);
            Assert.IsTrue(updatedSession.IsComplete);

            return updatedSession;
        }

        private LibraryFile CreateTestLibraryFile()
        {
            return new LibraryFile(
                id: _testFileId,
                libraryId: _testLibraryId,
                originalFileName: "test.txt",
                contentType: "text/plain",
                sizeInBytes: 1024,
                relativePath: $"/libraries/{_testLibraryId}/test.txt",
                hash: new byte[] { 0x01, 0x02, 0x03 },
                uploadedByUserId: _testUserId,
                uploadedAt: DateTime.UtcNow.AddMinutes(-5),
                createdAt: DateTime.UtcNow.AddMinutes(-5),
                updatedAt: DateTime.UtcNow.AddMinutes(-5)
            );
        }

        #endregion
    }
}

using EasyReasy.FileStorage;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;
using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Services.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Services.Hashing;
using Microsoft.Extensions.Caching.Memory;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Storage
{
    /// <summary>
    /// Implementation of file storage service using EasyReasy.FileStorage with PostgreSQL metadata storage and chunked uploads.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryFileRepository _fileRepository;
        private readonly ILibraryAuthorizationService _authorizationService;
        private readonly IFileHashService _fileHashService;
        private readonly IMemoryCache _sessionCache;
        private readonly ILogger<FileStorageService> _logger;
        private readonly long _maxFileSizeBytes;
        private const string SessionCacheKeyPrefix = "chunked_upload_session_";
        private static readonly TimeSpan SessionExpiry = TimeSpan.FromHours(24); // 24 hour session expiry

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStorageService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system for storage operations.</param>
        /// <param name="fileRepository">The repository for file metadata operations.</param>
        /// <param name="authorizationService">The service for handling library authorization.</param>
        /// <param name="fileHashService">The service for computing file hashes.</param>
        /// <param name="sessionCache">The memory cache for upload session management.</param>
        /// <param name="maxFileSizeBytes">The maximum file size in bytes that can be uploaded.</param>
        /// <param name="logger">The logger for logging operations.</param>
        public FileStorageService(
            IFileSystem fileSystem,
            ILibraryFileRepository fileRepository,
            ILibraryAuthorizationService authorizationService,
            IFileHashService fileHashService,
            IMemoryCache sessionCache,
            long maxFileSizeBytes,
            ILogger<FileStorageService> logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _fileHashService = fileHashService ?? throw new ArgumentNullException(nameof(fileHashService));
            _sessionCache = sessionCache ?? throw new ArgumentNullException(nameof(sessionCache));
            _maxFileSizeBytes = maxFileSizeBytes > 0 ? maxFileSizeBytes : throw new ArgumentException("Max file size must be greater than zero.", nameof(maxFileSizeBytes));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
            Guid libraryId,
            string fileName,
            string contentType,
            long totalSize,
            int chunkSize,
            Guid uploadedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

            if (totalSize <= 0)
                throw new ArgumentException("Total size must be greater than zero.", nameof(totalSize));

            if (totalSize > _maxFileSizeBytes)
                throw new ArgumentException($"File size ({FormatBytes(totalSize)}) exceeds maximum allowed size ({FormatBytes(_maxFileSizeBytes)}).", nameof(totalSize));

            if (chunkSize <= 0 || chunkSize > 50 * 1024 * 1024) // Max 50MB chunk size
                throw new ArgumentException("Chunk size must be between 1 and 50MB.", nameof(chunkSize));

            try
            {
                // Check write permission for the library
                await _authorizationService.ValidateAccessAsync(uploadedByUserId, libraryId, LibraryPermissionType.Write, "initiate file upload");

                // Ensure the library directory exists
                await EnsureLibraryExistsAsync(libraryId, cancellationToken);

                // Create upload session
                Guid sessionId = Guid.NewGuid();
                DateTime now = DateTime.UtcNow;
                DateTime expiresAt = now.Add(SessionExpiry);

                ChunkedUploadSession session = new(
                    sessionId: sessionId,
                    libraryId: libraryId,
                    originalFileName: fileName,
                    contentType: contentType,
                    totalSize: totalSize,
                    chunkSize: chunkSize,
                    uploadedByUserId: uploadedByUserId,
                    createdAt: now,
                    expiresAt: expiresAt
                );

                // Create temporary file for assembling chunks
                string tempPath = GetTempFilePath(sessionId);
                await _fileSystem.CreateDirectoryAsync(Path.GetDirectoryName(tempPath)!, cancellationToken);
                session.TempFilePath = tempPath;

                // Store session in cache
                string cacheKey = GetSessionCacheKey(sessionId);
                _sessionCache.Set(cacheKey, session, expiresAt);

                _logger.LogInformation("Initiated chunked upload session {SessionId} for file {FileName} in library {LibraryId}",
                    sessionId, fileName, libraryId);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate chunked upload for file {FileName} in library {LibraryId}",
                    fileName, libraryId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChunkedUploadSession> UploadChunkAsync(
            Guid sessionId,
            int chunkNumber,
            Stream chunkData,
            CancellationToken cancellationToken = default)
        {
            if (chunkData == null)
                throw new ArgumentNullException(nameof(chunkData));

            ChunkedUploadSession? session = await GetChunkedUploadSessionAsync(sessionId, cancellationToken);
            if (session == null)
                throw new InvalidOperationException($"Upload session {sessionId} not found or expired.");

            if (chunkNumber < 0 || chunkNumber >= session.TotalChunks)
                throw new ArgumentException($"Invalid chunk number {chunkNumber}. Expected 0-{session.TotalChunks - 1}.", nameof(chunkNumber));

            if (session.UploadedChunks.Contains(chunkNumber))
                throw new InvalidOperationException($"Chunk {chunkNumber} has already been uploaded.");

            try
            {
                // Calculate expected chunk size (last chunk might be smaller)
                long expectedChunkSize = chunkNumber == session.TotalChunks - 1
                    ? session.TotalSize - (chunkNumber * session.ChunkSize)
                    : session.ChunkSize;

                // Write chunk to temporary file at correct position
                using (Stream tempFileStream = await _fileSystem.OpenFileForWritingAsync(session.TempFilePath, append: false, cancellationToken))
                {
                    // Seek to the correct position for this chunk
                    tempFileStream.Seek(chunkNumber * session.ChunkSize, SeekOrigin.Begin);

                    // Copy chunk data
                    await chunkData.CopyToAsync(tempFileStream, cancellationToken);

                    // Verify the chunk size (for data integrity)
                    if (tempFileStream.Position - (chunkNumber * session.ChunkSize) != expectedChunkSize)
                    {
                        throw new InvalidOperationException($"Chunk {chunkNumber} size mismatch. Expected {expectedChunkSize} bytes.");
                    }
                }

                // Update session with uploaded chunk
                session.UploadedChunks.Add(chunkNumber);
                session.UploadedChunks.Sort(); // Keep chunks sorted for easy tracking

                // Update session in cache
                string cacheKey = GetSessionCacheKey(sessionId);
                _sessionCache.Set(cacheKey, session, session.ExpiresAt);

                _logger.LogDebug("Successfully uploaded chunk {ChunkNumber}/{TotalChunks} for session {SessionId}",
                    chunkNumber + 1, session.TotalChunks, sessionId);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload chunk {ChunkNumber} for session {SessionId}",
                    chunkNumber, sessionId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryFileDto> CompleteChunkedUploadAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ChunkedUploadSession? session = await GetChunkedUploadSessionAsync(sessionId, cancellationToken);
            if (session == null)
                throw new InvalidOperationException($"Upload session {sessionId} not found or expired.");

            if (!session.IsComplete)
                throw new InvalidOperationException($"Upload session {sessionId} is not complete. {session.UploadedChunks.Count}/{session.TotalChunks} chunks uploaded.");

            try
            {
                // Move assembled file to final location
                string libraryPath = GetLibraryPath(session.LibraryId);
                Guid fileId = Guid.NewGuid();
                string fileExtension = Path.GetExtension(session.OriginalFileName);
                string storedFileName = $"{fileId:N}{fileExtension}";
                string finalPath = Path.Combine(libraryPath, storedFileName);

                // Copy from temp file to final location
                using (Stream tempStream = await _fileSystem.OpenFileForReadingAsync(session.TempFilePath, cancellationToken))
                using (Stream finalStream = await _fileSystem.OpenFileForWritingAsync(finalPath, append: false, cancellationToken))
                {
                    await tempStream.CopyToAsync(finalStream, cancellationToken);
                }

                // Get final file size for verification
                long finalFileSize = await _fileSystem.GetFileSizeAsync(finalPath, cancellationToken);
                if (finalFileSize != session.TotalSize)
                {
                    // Clean up and throw error
                    await _fileSystem.DeleteFileAsync(finalPath, cancellationToken);
                    throw new InvalidOperationException($"File size mismatch after assembly. Expected {session.TotalSize} bytes, got {finalFileSize} bytes.");
                }

                // Compute hash of the final file
                byte[] fileHash;
                using (Stream fileStream = await _fileSystem.OpenFileForReadingAsync(finalPath, cancellationToken))
                {
                    fileHash = await _fileHashService.ComputeHashAsync(fileStream, cancellationToken);
                }

                // Store file metadata in database
                LibraryFile fileRecord = await _fileRepository.CreateAsync(
                    libraryId: session.LibraryId,
                    originalFileName: session.OriginalFileName,
                    contentType: session.ContentType,
                    sizeInBytes: finalFileSize,
                    relativePath: finalPath,
                    hash: fileHash,
                    uploadedByUserId: session.UploadedByUserId
                );

                // Clean up upload session and temp file
                await CleanupUploadSessionAsync(sessionId, cancellationToken);

                _logger.LogInformation("Successfully completed chunked upload for session {SessionId}. Final file ID: {FileId}",
                    sessionId, fileRecord.Id);

                return LibraryFileDto.FromFile(fileRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete chunked upload for session {SessionId}", sessionId);

                // Try to clean up on failure
                try
                {
                    await CleanupUploadSessionAsync(sessionId, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up upload session {SessionId} after completion failure", sessionId);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChunkedUploadSession?> GetChunkedUploadSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = GetSessionCacheKey(sessionId);
            if (_sessionCache.TryGetValue(cacheKey, out ChunkedUploadSession? session))
            {
                if (session != null && !session.IsExpired)
                {
                    return session;
                }

                // Remove expired session
                _sessionCache.Remove(cacheKey);
                if (session != null)
                {
                    try
                    {
                        await CleanupTempFileAsync(session.TempFilePath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up expired session {SessionId} temp file", sessionId);
                    }
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> CancelChunkedUploadAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            ChunkedUploadSession? session = await GetChunkedUploadSessionAsync(sessionId, cancellationToken);
            if (session == null)
                return false;

            await CleanupUploadSessionAsync(sessionId, cancellationToken);

            _logger.LogInformation("Cancelled chunked upload session {SessionId}", sessionId);
            return true;
        }

        /// <inheritdoc/>
        public async Task<LibraryFileDto?> GetFileInfoAsync(
            Guid libraryId,
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check read permission for the library
                await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Read, "access file information");

                LibraryFile? file = await _fileRepository.GetByIdInKnowledgeBaseAsync(libraryId, fileId);
                return file == null ? null : LibraryFileDto.FromFile(file);
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get file info for file {FileId} in library {LibraryId}",
                    fileId, libraryId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> GetFileStreamAsync(
            Guid libraryId,
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Check read permission for the library
            await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Read, "access file content");

            LibraryFile? file = await _fileRepository.GetByIdInKnowledgeBaseAsync(libraryId, fileId);
            if (file == null)
                throw new FileNotFoundException($"File {fileId} not found in library {libraryId}");

            return await _fileSystem.OpenFileForReadingAsync(file.RelativePath, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<string> GetFileContentAsync(
            Guid libraryId,
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Check read permission for the library
            await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Read, "access file content");

            LibraryFile? file = await _fileRepository.GetByIdInKnowledgeBaseAsync(libraryId, fileId);
            if (file == null)
                throw new FileNotFoundException($"File {fileId} not found in library {libraryId}");

            return await _fileSystem.ReadFileAsTextAsync(file.RelativePath, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteFileAsync(
            Guid libraryId,
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check write permission for the library (users can delete files with write permission)
                await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Write, "delete file");

                LibraryFile? file = await _fileRepository.GetByIdInKnowledgeBaseAsync(libraryId, fileId);
                if (file == null)
                    return false;

                // Delete the actual file
                if (await _fileSystem.FileExistsAsync(file.RelativePath, cancellationToken))
                {
                    await _fileSystem.DeleteFileAsync(file.RelativePath, cancellationToken);
                }

                // Delete the database record
                bool deleted = await _fileRepository.DeleteAsync(fileId);

                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted file {FileId} from library {LibraryId}",
                        fileId, libraryId);
                }

                return deleted;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId} from library {LibraryId}",
                    fileId, libraryId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<LibraryFileDto>> ListFilesAsync(
            Guid libraryId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check read permission for the library
                await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Read, "list files");

                List<LibraryFile> files = await _fileRepository.GetByKnowledgeBaseIdAsync(libraryId);
                return files.Select(LibraryFileDto.FromFile).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files in library {LibraryId}", libraryId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> FileExistsAsync(
            Guid libraryId,
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Check read permission for the library
            await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Read, "check file existence");

            return await _fileRepository.ExistsInKnowledgeBaseAsync(libraryId, fileId);
        }

        /// <inheritdoc/>
        public async Task EnsureLibraryExistsAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default)
        {
            string libraryPath = GetLibraryPath(libraryId);

            if (!await _fileSystem.DirectoryExistsAsync(libraryPath, cancellationToken))
            {
                await _fileSystem.CreateDirectoryAsync(libraryPath, cancellationToken);
                _logger.LogInformation("Created library directory for {LibraryId}", libraryId);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteLibraryAsync(
            Guid libraryId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check admin permission for the library (only admins can delete entire librarys)
                await _authorizationService.ValidateAccessAsync(userId, libraryId, LibraryPermissionType.Admin, "delete library");

                // Delete all file records from database
                int deletedRecords = await _fileRepository.DeleteByKnowledgeBaseIdAsync(libraryId);

                // Delete the library directory and all files
                string libraryPath = GetLibraryPath(libraryId);
                if (await _fileSystem.DirectoryExistsAsync(libraryPath, cancellationToken))
                {
                    await _fileSystem.DeleteDirectoryAsync(libraryPath, deleteNonEmpty: true, cancellationToken);
                }

                bool hadFiles = deletedRecords > 0;

                _logger.LogInformation("Successfully deleted library {LibraryId} with {FileCount} files",
                    libraryId, deletedRecords);

                return hadFiles;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Re-throw authorization exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete library {LibraryId}", libraryId);
                throw;
            }
        }

        private string GetLibraryPath(Guid libraryId)
        {
            return $"lib_{libraryId:N}";
        }

        private string GetTempFilePath(Guid sessionId)
        {
            return Path.Combine("temp", $"upload_{sessionId:N}.tmp");
        }

        private string GetSessionCacheKey(Guid sessionId)
        {
            return $"{SessionCacheKeyPrefix}{sessionId:N}";
        }

        private async Task CleanupUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            // Remove from cache
            string cacheKey = GetSessionCacheKey(sessionId);
            if (_sessionCache.TryGetValue(cacheKey, out ChunkedUploadSession? session))
            {
                _sessionCache.Remove(cacheKey);
                if (session != null)
                {
                    await CleanupTempFileAsync(session.TempFilePath, cancellationToken);
                }
            }
        }

        private async Task CleanupTempFileAsync(string tempFilePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tempFilePath))
                return;

            try
            {
                if (await _fileSystem.FileExistsAsync(tempFilePath, cancellationToken))
                {
                    await _fileSystem.DeleteFileAsync(tempFilePath, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file {TempFilePath}", tempFilePath);
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0)
                return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
    }
}

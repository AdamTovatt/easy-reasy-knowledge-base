using EasyReasy.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;
using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyReasy.KnowledgeBase.Web.Server.Controllers
{
    /// <summary>
    /// Controller for file storage operations with chunked upload support.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStorageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileStorageController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStorageController"/> class.
        /// </summary>
        /// <param name="fileStorageService">The file storage service.</param>
        /// <param name="logger">The logger for logging operations.</param>
        public FileStorageController(IFileStorageService fileStorageService, ILogger<FileStorageController> logger)
        {
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates a chunked upload session for a file.
        /// </summary>
        /// <param name="request">The chunked upload initiation request.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The created upload session information.</returns>
        [HttpPost("initiate-chunked-upload")]
        public async Task<IActionResult> InitiateChunkedUpload([FromBody] InitiateChunkedUploadRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request body is required");

            try
            {
                // Get user ID from JWT token
                string? userIdString = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Unauthorized("Invalid user authentication");

                ChunkedUploadSession session = await _fileStorageService.InitiateChunkedUploadAsync(
                    knowledgeBaseId: request.KnowledgeBaseId,
                    fileName: request.FileName,
                    contentType: request.ContentType,
                    totalSize: request.TotalSize,
                    chunkSize: request.ChunkSize,
                    uploadedByUserId: userId,
                    cancellationToken: cancellationToken
                );

                ChunkedUploadResponse response = ChunkedUploadResponse.FromSession(session);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate chunked upload for file {FileName}", request.FileName);
                return StatusCode(500, "An error occurred while initiating the upload");
            }
        }

        /// <summary>
        /// Uploads a chunk of data to an existing upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="chunkNumber">The zero-based index of the chunk being uploaded.</param>
        /// <param name="chunkFile">The chunk data file.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The updated upload session information.</returns>
        [HttpPost("upload-chunk/{sessionId:guid}")]
        public async Task<IActionResult> UploadChunk(Guid sessionId, [FromForm] int chunkNumber, [FromForm] IFormFile chunkFile, CancellationToken cancellationToken)
        {
            if (chunkFile == null || chunkFile.Length == 0)
                return BadRequest("Chunk file is required and cannot be empty");

            if (chunkNumber < 0)
                return BadRequest("Chunk number must be non-negative");

            try
            {
                using (Stream chunkStream = chunkFile.OpenReadStream())
                {
                    ChunkedUploadSession session = await _fileStorageService.UploadChunkAsync(
                        sessionId: sessionId,
                        chunkNumber: chunkNumber,
                        chunkData: chunkStream,
                        cancellationToken: cancellationToken
                    );

                    ChunkedUploadResponse response = ChunkedUploadResponse.FromSession(session);
                    return Ok(response);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload chunk {ChunkNumber} for session {SessionId}", chunkNumber, sessionId);
                return StatusCode(500, "An error occurred while uploading the chunk");
            }
        }

        /// <summary>
        /// Completes a chunked upload session and creates the final file.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Information about the completed file upload.</returns>
        [HttpPost("complete-chunked-upload/{sessionId:guid}")]
        public async Task<IActionResult> CompleteChunkedUpload(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                KnowledgeFileDto fileInfo = await _fileStorageService.CompleteChunkedUploadAsync(sessionId, cancellationToken);
                FileUploadResponse response = new FileUploadResponse(
                    success: true,
                    message: "File uploaded successfully",
                    fileInfo: fileInfo
                );

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete chunked upload for session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while completing the upload");
            }
        }

        /// <summary>
        /// Gets the status of a chunked upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The upload session status.</returns>
        [HttpGet("upload-status/{sessionId:guid}")]
        public async Task<IActionResult> GetUploadStatus(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                ChunkedUploadSession? session = await _fileStorageService.GetChunkedUploadSessionAsync(sessionId, cancellationToken);
                if (session == null)
                    return NotFound("Upload session not found or expired");

                ChunkedUploadResponse response = ChunkedUploadResponse.FromSession(session);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get upload status for session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while retrieving upload status");
            }
        }

        /// <summary>
        /// Cancels and cleans up a chunked upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Confirmation of cancellation.</returns>
        [HttpDelete("cancel-upload/{sessionId:guid}")]
        public async Task<IActionResult> CancelChunkedUpload(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                bool cancelled = await _fileStorageService.CancelChunkedUploadAsync(sessionId, cancellationToken);
                if (!cancelled)
                    return NotFound("Upload session not found or already completed");

                return Ok(new { message = "Upload session cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel upload for session {SessionId}", sessionId);
                return StatusCode(500, "An error occurred while cancelling the upload");
            }
        }

        /// <summary>
        /// Lists all files in a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of files in the knowledge base.</returns>
        [HttpGet("files/{knowledgeBaseId:guid}")]
        public async Task<IActionResult> ListFiles(Guid knowledgeBaseId, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token
                string? userIdString = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Unauthorized("Invalid user authentication");

                IEnumerable<KnowledgeFileDto> files = await _fileStorageService.ListFilesAsync(knowledgeBaseId, userId, cancellationToken);
                return Ok(files);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to list files in knowledge base {KnowledgeBaseId}", knowledgeBaseId);
                return Forbid("Access denied. You don't have permission to list files in this knowledge base.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files in knowledge base {KnowledgeBaseId}", knowledgeBaseId);
                return StatusCode(500, "An error occurred while listing files");
            }
        }

        /// <summary>
        /// Gets information about a specific file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Information about the file.</returns>
        [HttpGet("files/{knowledgeBaseId:guid}/{fileId:guid}")]
        public async Task<IActionResult> GetFileInfo(Guid knowledgeBaseId, Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token
                string? userIdString = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Unauthorized("Invalid user authentication");

                KnowledgeFileDto? fileInfo = await _fileStorageService.GetFileInfoAsync(knowledgeBaseId, fileId, userId, cancellationToken);
                if (fileInfo == null)
                    return NotFound("File not found");

                return Ok(fileInfo);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to file info {FileId} in knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return Forbid("Access denied. You don't have permission to access this file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file info for file {FileId} in knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return StatusCode(500, "An error occurred while retrieving file information");
            }
        }

        /// <summary>
        /// Downloads a file from the knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The file content stream.</returns>
        [HttpGet("download/{knowledgeBaseId:guid}/{fileId:guid}")]
        public async Task<IActionResult> DownloadFile(Guid knowledgeBaseId, Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token
                string? userIdString = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Unauthorized("Invalid user authentication");

                KnowledgeFileDto? fileInfo = await _fileStorageService.GetFileInfoAsync(knowledgeBaseId, fileId, userId, cancellationToken);
                if (fileInfo == null)
                    return NotFound("File not found");

                Stream fileStream = await _fileStorageService.GetFileStreamAsync(knowledgeBaseId, fileId, userId, cancellationToken);
                
                return File(fileStream, fileInfo.ContentType, fileInfo.OriginalFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized download attempt for file {FileId} in knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return Forbid("Access denied. You don't have permission to download this file.");
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file {FileId} from knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return StatusCode(500, "An error occurred while downloading the file");
            }
        }

        /// <summary>
        /// Deletes a file from the knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Confirmation of deletion.</returns>
        [HttpDelete("files/{knowledgeBaseId:guid}/{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid knowledgeBaseId, Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                // Get user ID from JWT token
                string? userIdString = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                    return Unauthorized("Invalid user authentication");

                bool deleted = await _fileStorageService.DeleteFileAsync(knowledgeBaseId, fileId, userId, cancellationToken);
                if (!deleted)
                    return NotFound("File not found");

                return Ok(new { message = "File deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt for file {FileId} in knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return Forbid("Access denied. You don't have permission to delete this file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileId} from knowledge base {KnowledgeBaseId}", fileId, knowledgeBaseId);
                return StatusCode(500, "An error occurred while deleting the file");
            }
        }
    }
}

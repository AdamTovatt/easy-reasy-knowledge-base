using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Storage
{
    /// <summary>
    /// Interface for file storage operations with support for multiple knowledge bases.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Initiates a chunked upload session for a file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileName">The name of the file to upload.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="totalSize">The total size of the file in bytes.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user uploading the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The created upload session information.</returns>
        Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
            Guid knowledgeBaseId,
            string fileName,
            string contentType,
            long totalSize,
            int chunkSize,
            Guid uploadedByUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a chunk of data to an existing upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="chunkNumber">The zero-based index of the chunk being uploaded.</param>
        /// <param name="chunkData">The chunk data stream.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The updated upload session information.</returns>
        Task<ChunkedUploadSession> UploadChunkAsync(
            Guid sessionId,
            int chunkNumber,
            Stream chunkData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes a chunked upload session and creates the final file.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Information about the completed file upload.</returns>
        Task<KnowledgeFileDto> CompleteChunkedUploadAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a chunked upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The upload session information, or null if not found.</returns>
        Task<ChunkedUploadSession?> GetChunkedUploadSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels and cleans up a chunked upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the session was cancelled, false if it didn't exist.</returns>
        Task<bool> CancelChunkedUploadAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="userId">The unique identifier of the user requesting access.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Information about the stored file, or null if not found.</returns>
        Task<KnowledgeFileDto?> GetFileInfoAsync(
            Guid knowledgeBaseId, 
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a stream to read a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="userId">The unique identifier of the user requesting access.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A stream to read the file content.</returns>
        Task<Stream> GetFileStreamAsync(
            Guid knowledgeBaseId, 
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content of a stored file as text.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="userId">The unique identifier of the user requesting access.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The file content as text.</returns>
        Task<string> GetFileContentAsync(
            Guid knowledgeBaseId, 
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="userId">The unique identifier of the user requesting the deletion.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the file was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteFileAsync(
            Guid knowledgeBaseId, 
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all files in a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user requesting the file list.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A collection of file information objects.</returns>
        Task<IEnumerable<KnowledgeFileDto>> ListFilesAsync(
            Guid knowledgeBaseId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists in the knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="userId">The unique identifier of the user checking file existence.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> FileExistsAsync(
            Guid knowledgeBaseId, 
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a knowledge base directory if it doesn't exist.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        Task EnsureKnowledgeBaseExistsAsync(
            Guid knowledgeBaseId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entire knowledge base and all its files.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user requesting the deletion (must have admin permission).</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the knowledge base was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteKnowledgeBaseAsync(
            Guid knowledgeBaseId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}

using EasyReasy.KnowledgeBase.Web.Server.Models.Storage;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Storage
{
    /// <summary>
    /// Interface for file storage operations with support for multiple knowledge bases.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Stores a file in the specified knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileName">The name of the file to store.</param>
        /// <param name="fileStream">The file content stream.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Information about the stored file.</returns>
        Task<StoredFileInfo> StoreFileAsync(
            string knowledgeBaseId, 
            string fileName, 
            Stream fileStream, 
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Information about the stored file, or null if not found.</returns>
        Task<StoredFileInfo?> GetFileInfoAsync(
            string knowledgeBaseId, 
            string fileId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a stream to read a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A stream to read the file content.</returns>
        Task<Stream> GetFileStreamAsync(
            string knowledgeBaseId, 
            string fileId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content of a stored file as text.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The file content as text.</returns>
        Task<string> GetFileContentAsync(
            string knowledgeBaseId, 
            string fileId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a stored file.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the file was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteFileAsync(
            string knowledgeBaseId, 
            string fileId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all files in a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A collection of file information objects.</returns>
        Task<IEnumerable<StoredFileInfo>> ListFilesAsync(
            string knowledgeBaseId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists in the knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> FileExistsAsync(
            string knowledgeBaseId, 
            string fileId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a knowledge base directory if it doesn't exist.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        Task EnsureKnowledgeBaseExistsAsync(
            string knowledgeBaseId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entire knowledge base and all its files.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if the knowledge base was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteKnowledgeBaseAsync(
            string knowledgeBaseId, 
            CancellationToken cancellationToken = default);
    }
}

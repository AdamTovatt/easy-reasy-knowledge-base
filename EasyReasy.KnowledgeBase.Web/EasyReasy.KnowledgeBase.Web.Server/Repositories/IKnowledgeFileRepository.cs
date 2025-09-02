using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for file data access operations.
    /// </summary>
    public interface IKnowledgeFileRepository
    {
        /// <summary>
        /// Creates a new file record in the database.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="originalFileName">The original name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="sizeInBytes">The size of the file in bytes.</param>
        /// <param name="relativePath">The relative path to the file.</param>
        /// <param name="hash">The SHA-256 hash of the file content.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user who uploaded the file.</param>
        /// <returns>The created file record with populated ID and timestamps.</returns>
        Task<KnowledgeFile> CreateAsync(
            Guid knowledgeBaseId,
            string originalFileName,
            string contentType,
            long sizeInBytes,
            string relativePath,
            byte[] hash,
            Guid uploadedByUserId);

        /// <summary>
        /// Retrieves a file record by its unique identifier.
        /// </summary>
        /// <param name="id">The file's unique identifier.</param>
        /// <returns>The file record if found, null otherwise.</returns>
        Task<KnowledgeFile?> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves a file record by its unique identifier within a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The file's unique identifier.</param>
        /// <returns>The file record if found, null otherwise.</returns>
        Task<KnowledgeFile?> GetByIdInKnowledgeBaseAsync(Guid knowledgeBaseId, Guid fileId);

        /// <summary>
        /// Updates an existing file record in the database.
        /// </summary>
        /// <param name="file">The file record to update.</param>
        /// <returns>The updated file record.</returns>
        Task<KnowledgeFile> UpdateAsync(KnowledgeFile file);

        /// <summary>
        /// Deletes a file record from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the file to delete.</param>
        /// <returns>True if the file was deleted, false if not found.</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Retrieves all file records for a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <returns>A list of all files in the knowledge base.</returns>
        Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId);

        /// <summary>
        /// Retrieves all file records for a specific knowledge base with pagination.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="offset">The number of records to skip.</param>
        /// <param name="limit">The maximum number of records to return.</param>
        /// <returns>A list of files in the knowledge base.</returns>
        Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAsync(Guid knowledgeBaseId, int offset, int limit);

        /// <summary>
        /// Retrieves all file records uploaded by a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of all files uploaded by the user.</returns>
        Task<List<KnowledgeFile>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Retrieves all file records uploaded by a specific user in a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of files uploaded by the user in the knowledge base.</returns>
        Task<List<KnowledgeFile>> GetByKnowledgeBaseIdAndUserIdAsync(Guid knowledgeBaseId, Guid userId);

        /// <summary>
        /// Checks if a file record exists by its unique identifier.
        /// </summary>
        /// <param name="id">The file's unique identifier.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Checks if a file record exists within a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="fileId">The file's unique identifier.</param>
        /// <returns>True if the file exists in the knowledge base, false otherwise.</returns>
        Task<bool> ExistsInKnowledgeBaseAsync(Guid knowledgeBaseId, Guid fileId);

        /// <summary>
        /// Gets the total count of files in a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <returns>The total number of files in the knowledge base.</returns>
        Task<long> GetCountByKnowledgeBaseIdAsync(Guid knowledgeBaseId);

        /// <summary>
        /// Gets the total size in bytes of all files in a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <returns>The total size in bytes of all files in the knowledge base.</returns>
        Task<long> GetTotalSizeByKnowledgeBaseIdAsync(Guid knowledgeBaseId);

        /// <summary>
        /// Deletes all file records for a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <returns>The number of files deleted.</returns>
        Task<int> DeleteByKnowledgeBaseIdAsync(Guid knowledgeBaseId);
    }
}

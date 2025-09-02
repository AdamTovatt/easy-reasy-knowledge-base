using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for knowledge base data access operations.
    /// </summary>
    public interface IKnowledgeBaseRepository
    {
        /// <summary>
        /// Creates a new knowledge base.
        /// </summary>
        /// <param name="name">The name of the knowledge base.</param>
        /// <param name="description">The description of the knowledge base.</param>
        /// <param name="ownerId">The unique identifier of the user who will own the knowledge base.</param>
        /// <param name="isPublic">Whether the knowledge base should be publicly readable.</param>
        /// <returns>The created knowledge base.</returns>
        Task<Models.KnowledgeBase> CreateAsync(string name, string? description, Guid ownerId, bool isPublic = false);

        /// <summary>
        /// Gets a knowledge base by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the knowledge base.</param>
        /// <returns>The knowledge base if found, null otherwise.</returns>
        Task<Models.KnowledgeBase?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a knowledge base by its name.
        /// </summary>
        /// <param name="name">The name of the knowledge base.</param>
        /// <returns>The knowledge base if found, null otherwise.</returns>
        Task<Models.KnowledgeBase?> GetByNameAsync(string name);

        /// <summary>
        /// Updates an existing knowledge base.
        /// </summary>
        /// <param name="knowledgeBase">The knowledge base to update.</param>
        /// <returns>The updated knowledge base.</returns>
        Task<Models.KnowledgeBase> UpdateAsync(Models.KnowledgeBase knowledgeBase);

        /// <summary>
        /// Deletes a knowledge base by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the knowledge base to delete.</param>
        /// <returns>True if the knowledge base was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Gets all knowledge bases owned by a specific user.
        /// </summary>
        /// <param name="ownerId">The unique identifier of the owner.</param>
        /// <returns>A list of knowledge bases owned by the user.</returns>
        Task<List<Models.KnowledgeBase>> GetByOwnerIdAsync(Guid ownerId);

        /// <summary>
        /// Gets all public knowledge bases.
        /// </summary>
        /// <returns>A list of all public knowledge bases.</returns>
        Task<List<Models.KnowledgeBase>> GetPublicKnowledgeBasesAsync();

        /// <summary>
        /// Checks if a knowledge base exists.
        /// </summary>
        /// <param name="id">The unique identifier of the knowledge base.</param>
        /// <returns>True if the knowledge base exists, false otherwise.</returns>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Checks if a user is the owner of a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>True if the user is the owner, false otherwise.</returns>
        Task<bool> IsOwnerAsync(Guid knowledgeBaseId, Guid userId);
    }
}

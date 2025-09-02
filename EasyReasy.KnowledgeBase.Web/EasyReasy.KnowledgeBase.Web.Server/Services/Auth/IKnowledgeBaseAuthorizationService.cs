using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Auth
{
    /// <summary>
    /// Service for handling knowledge base authorization logic.
    /// </summary>
    public interface IKnowledgeBaseAuthorizationService
    {
        /// <summary>
        /// Checks if a user has the required permission for a knowledge base.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="requiredPermission">The minimum permission level required.</param>
        /// <returns>True if the user has sufficient permission, false otherwise.</returns>
        Task<bool> HasPermissionAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType requiredPermission);

        /// <summary>
        /// Gets the effective permission level a user has for a knowledge base.
        /// Takes into account ownership, public status, and explicit permissions.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>The effective permission level, or null if no access.</returns>
        Task<KnowledgeBasePermissionType?> GetEffectivePermissionAsync(Guid userId, Guid knowledgeBaseId);

        /// <summary>
        /// Gets all knowledge base IDs that a user can access with the specified minimum permission.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="minimumPermission">The minimum permission level required.</param>
        /// <returns>A list of accessible knowledge base IDs.</returns>
        Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, KnowledgeBasePermissionType minimumPermission = KnowledgeBasePermissionType.Read);

        /// <summary>
        /// Validates that a user can perform a specific action on a knowledge base.
        /// Throws UnauthorizedAccessException if access is denied.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="requiredPermission">The minimum permission level required.</param>
        /// <param name="actionDescription">Description of the action being performed (for error messages).</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user lacks sufficient permission.</exception>
        Task ValidateAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType requiredPermission, string actionDescription);

        /// <summary>
        /// Checks if a user is the owner of a knowledge base.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>True if the user is the owner, false otherwise.</returns>
        Task<bool> IsOwnerAsync(Guid userId, Guid knowledgeBaseId);

        /// <summary>
        /// Checks if a knowledge base is publicly accessible for read operations.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>True if the knowledge base is public, false otherwise.</returns>
        Task<bool> IsPublicAsync(Guid knowledgeBaseId);
    }
}

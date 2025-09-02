using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for knowledge base permission data access operations.
    /// </summary>
    public interface IKnowledgeBasePermissionRepository
    {
        /// <summary>
        /// Checks if a user has a specific permission for a knowledge base.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="permissionType">The type of permission to check.</param>
        /// <returns>True if the user has the specified permission, false otherwise.</returns>
        Task<bool> HasPermissionAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType permissionType);

        /// <summary>
        /// Gets the highest permission level a user has for a knowledge base.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>The highest permission type, or null if no permission exists.</returns>
        Task<KnowledgeBasePermissionType?> GetUserPermissionAsync(Guid userId, Guid knowledgeBaseId);

        /// <summary>
        /// Gets all knowledge base IDs that a user has access to with the specified permission or higher.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="minimumPermission">The minimum permission level required.</param>
        /// <returns>A list of knowledge base IDs the user can access.</returns>
        Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, KnowledgeBasePermissionType minimumPermission = KnowledgeBasePermissionType.Read);

        /// <summary>
        /// Grants a permission to a user for a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user to grant permission to.</param>
        /// <param name="permissionType">The type of permission to grant.</param>
        /// <param name="grantedByUserId">The unique identifier of the user granting the permission.</param>
        /// <returns>The created permission record.</returns>
        Task<KnowledgeBasePermission> GrantPermissionAsync(Guid knowledgeBaseId, Guid userId, KnowledgeBasePermissionType permissionType, Guid grantedByUserId);

        /// <summary>
        /// Updates an existing permission for a user and knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="newPermissionType">The new permission type to assign.</param>
        /// <param name="updatedByUserId">The unique identifier of the user making the update.</param>
        /// <returns>The updated permission record, or null if no existing permission was found.</returns>
        Task<KnowledgeBasePermission?> UpdatePermissionAsync(Guid knowledgeBaseId, Guid userId, KnowledgeBasePermissionType newPermissionType, Guid updatedByUserId);

        /// <summary>
        /// Revokes a user's permission for a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <param name="userId">The unique identifier of the user to revoke permission from.</param>
        /// <returns>True if a permission was revoked, false if no permission existed.</returns>
        Task<bool> RevokePermissionAsync(Guid knowledgeBaseId, Guid userId);

        /// <summary>
        /// Gets all permissions for a specific knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>A list of all permissions for the knowledge base.</returns>
        Task<List<KnowledgeBasePermission>> GetPermissionsByKnowledgeBaseAsync(Guid knowledgeBaseId);

        /// <summary>
        /// Gets all permissions granted to a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of all permissions granted to the user.</returns>
        Task<List<KnowledgeBasePermission>> GetPermissionsByUserAsync(Guid userId);

        /// <summary>
        /// Revokes all permissions for a knowledge base (useful when deleting a knowledge base).
        /// </summary>
        /// <param name="knowledgeBaseId">The unique identifier of the knowledge base.</param>
        /// <returns>The number of permissions that were revoked.</returns>
        Task<int> RevokeAllPermissionsForKnowledgeBaseAsync(Guid knowledgeBaseId);

        /// <summary>
        /// Revokes all permissions granted to a specific user (useful when deleting a user).
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The number of permissions that were revoked.</returns>
        Task<int> RevokeAllPermissionsForUserAsync(Guid userId);
    }
}

using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for library permission data access operations.
    /// </summary>
    public interface ILibraryPermissionRepository
    {
        /// <summary>
        /// Checks if a user has a specific permission for a library.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="permissionType">The type of permission to check.</param>
        /// <returns>True if the user has the specified permission, false otherwise.</returns>
        Task<bool> HasPermissionAsync(Guid userId, Guid libraryId, LibraryPermissionType permissionType);

        /// <summary>
        /// Gets the highest permission level a user has for a library.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>The highest permission type, or null if no permission exists.</returns>
        Task<LibraryPermissionType?> GetUserPermissionAsync(Guid userId, Guid libraryId);

        /// <summary>
        /// Gets all library IDs that a user has access to with the specified permission or higher.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="minimumPermission">The minimum permission level required.</param>
        /// <returns>A list of library IDs the user can access.</returns>
        Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, LibraryPermissionType minimumPermission = LibraryPermissionType.Read);

        /// <summary>
        /// Grants a permission to a user for a library.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="userId">The unique identifier of the user to grant permission to.</param>
        /// <param name="permissionType">The type of permission to grant.</param>
        /// <param name="grantedByUserId">The unique identifier of the user granting the permission.</param>
        /// <returns>The created permission record.</returns>
        Task<LibraryPermission> GrantPermissionAsync(Guid libraryId, Guid userId, LibraryPermissionType permissionType, Guid grantedByUserId);

        /// <summary>
        /// Updates an existing permission for a user and library.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="newPermissionType">The new permission type to assign.</param>
        /// <param name="updatedByUserId">The unique identifier of the user making the update.</param>
        /// <returns>The updated permission record, or null if no existing permission was found.</returns>
        Task<LibraryPermission?> UpdatePermissionAsync(Guid libraryId, Guid userId, LibraryPermissionType newPermissionType, Guid updatedByUserId);

        /// <summary>
        /// Revokes a user's permission for a library.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="userId">The unique identifier of the user to revoke permission from.</param>
        /// <returns>True if a permission was revoked, false if no permission existed.</returns>
        Task<bool> RevokePermissionAsync(Guid libraryId, Guid userId);

        /// <summary>
        /// Gets all permissions for a specific library.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>A list of all permissions for the library.</returns>
        Task<List<LibraryPermission>> GetPermissionsByKnowledgeBaseAsync(Guid libraryId);

        /// <summary>
        /// Gets all permissions granted to a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of all permissions granted to the user.</returns>
        Task<List<LibraryPermission>> GetPermissionsByUserAsync(Guid userId);

        /// <summary>
        /// Revokes all permissions for a library (useful when deleting a library).
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>The number of permissions that were revoked.</returns>
        Task<int> RevokeAllPermissionsForKnowledgeBaseAsync(Guid libraryId);

        /// <summary>
        /// Revokes all permissions granted to a specific user (useful when deleting a user).
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The number of permissions that were revoked.</returns>
        Task<int> RevokeAllPermissionsForUserAsync(Guid userId);
    }
}

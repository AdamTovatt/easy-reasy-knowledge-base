using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Auth
{
    /// <summary>
    /// Service for handling library authorization logic.
    /// </summary>
    public interface ILibraryAuthorizationService
    {
        /// <summary>
        /// Checks if a user has the required permission for a library.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="requiredPermission">The minimum permission level required.</param>
        /// <returns>True if the user has sufficient permission, false otherwise.</returns>
        Task<bool> HasPermissionAsync(Guid userId, Guid libraryId, LibraryPermissionType requiredPermission);

        /// <summary>
        /// Gets the effective permission level a user has for a library.
        /// Takes into account ownership, public status, and explicit permissions.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>The effective permission level, or null if no access.</returns>
        Task<LibraryPermissionType?> GetEffectivePermissionAsync(Guid userId, Guid libraryId);

        /// <summary>
        /// Gets all library IDs that a user can access with the specified minimum permission.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="minimumPermission">The minimum permission level required.</param>
        /// <returns>A list of accessible library IDs.</returns>
        Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, LibraryPermissionType minimumPermission = LibraryPermissionType.Read);

        /// <summary>
        /// Validates that a user can perform a specific action on a library.
        /// Throws UnauthorizedAccessException if access is denied.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="requiredPermission">The minimum permission level required.</param>
        /// <param name="actionDescription">Description of the action being performed (for error messages).</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the user lacks sufficient permission.</exception>
        Task ValidateAccessAsync(Guid userId, Guid libraryId, LibraryPermissionType requiredPermission, string actionDescription);

        /// <summary>
        /// Checks if a user is the owner of a library.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>True if the user is the owner, false otherwise.</returns>
        Task<bool> IsOwnerAsync(Guid userId, Guid libraryId);

        /// <summary>
        /// Checks if a library is publicly accessible for read operations.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <returns>True if the library is public, false otherwise.</returns>
        Task<bool> IsPublicAsync(Guid libraryId);
    }
}

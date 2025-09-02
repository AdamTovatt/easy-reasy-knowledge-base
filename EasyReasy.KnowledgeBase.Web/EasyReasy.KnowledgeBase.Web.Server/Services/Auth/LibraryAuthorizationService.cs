using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Auth
{
    /// <summary>
    /// Service for handling library authorization logic using repositories.
    /// </summary>
    public class LibraryAuthorizationService : ILibraryAuthorizationService
    {
        private readonly ILibraryRepository _knowledgeBaseRepository;
        private readonly ILibraryPermissionRepository _permissionRepository;
        private readonly ILogger<LibraryAuthorizationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryAuthorizationService"/> class.
        /// </summary>
        /// <param name="knowledgeBaseRepository">Repository for library operations.</param>
        /// <param name="permissionRepository">Repository for permission operations.</param>
        /// <param name="logger">Logger for this service.</param>
        public LibraryAuthorizationService(
            ILibraryRepository knowledgeBaseRepository,
            ILibraryPermissionRepository permissionRepository,
            ILogger<LibraryAuthorizationService> logger)
        {
            _knowledgeBaseRepository = knowledgeBaseRepository ?? throw new ArgumentNullException(nameof(knowledgeBaseRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> HasPermissionAsync(Guid userId, Guid libraryId, LibraryPermissionType requiredPermission)
        {
            try
            {
                return await _permissionRepository.HasPermissionAsync(userId, libraryId, requiredPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for user {UserId} on library {KnowledgeBaseId}", userId, libraryId);
                return false; // Fail closed - deny access on error
            }
        }

        /// <inheritdoc/>
        public async Task<LibraryPermissionType?> GetEffectivePermissionAsync(Guid userId, Guid libraryId)
        {
            try
            {
                // Check if user is owner (always gets admin permissions)
                if (await _knowledgeBaseRepository.IsOwnerAsync(libraryId, userId))
                {
                    return LibraryPermissionType.Admin;
                }

                // Check if library exists and is public
                Library? knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(libraryId);
                if (knowledgeBase == null)
                {
                    return null; // Knowledge base doesn't exist
                }

                // Check explicit permission
                LibraryPermissionType? explicitPermission = await _permissionRepository.GetUserPermissionAsync(userId, libraryId);
                if (explicitPermission.HasValue)
                {
                    return explicitPermission.Value;
                }

                // Check if public (read-only access)
                if (knowledgeBase.IsPublic)
                {
                    return LibraryPermissionType.Read;
                }

                return null; // No access
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting effective permission for user {UserId} on library {KnowledgeBaseId}", userId, libraryId);
                return null; // Fail closed - deny access on error
            }
        }

        /// <inheritdoc/>
        public async Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, LibraryPermissionType minimumPermission = LibraryPermissionType.Read)
        {
            try
            {
                return await _permissionRepository.GetAccessibleKnowledgeBaseIdsAsync(userId, minimumPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible libraries for user {UserId} with minimum permission {MinimumPermission}", userId, minimumPermission);
                return new List<Guid>(); // Return empty list on error
            }
        }

        /// <inheritdoc/>
        public async Task ValidateAccessAsync(Guid userId, Guid libraryId, LibraryPermissionType requiredPermission, string actionDescription)
        {
            bool hasPermission = await HasPermissionAsync(userId, libraryId, requiredPermission);
            if (!hasPermission)
            {
                _logger.LogWarning("Access denied: User {UserId} attempted to {ActionDescription} on library {KnowledgeBaseId} but lacks {RequiredPermission} permission",
                    userId, actionDescription, libraryId, requiredPermission);

                throw new UnauthorizedAccessException($"Access denied. {requiredPermission} permission required to {actionDescription}.");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsOwnerAsync(Guid userId, Guid libraryId)
        {
            try
            {
                return await _knowledgeBaseRepository.IsOwnerAsync(libraryId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ownership for user {UserId} on library {KnowledgeBaseId}", userId, libraryId);
                return false; // Fail closed
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsPublicAsync(Guid libraryId)
        {
            try
            {
                Library? knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(libraryId);
                return knowledgeBase?.IsPublic ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if library {KnowledgeBaseId} is public", libraryId);
                return false; // Fail closed
            }
        }
    }
}

using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Auth
{
    /// <summary>
    /// Service for handling knowledge base authorization logic using repositories.
    /// </summary>
    public class KnowledgeBaseAuthorizationService : IKnowledgeBaseAuthorizationService
    {
        private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
        private readonly IKnowledgeBasePermissionRepository _permissionRepository;
        private readonly ILogger<KnowledgeBaseAuthorizationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeBaseAuthorizationService"/> class.
        /// </summary>
        /// <param name="knowledgeBaseRepository">Repository for knowledge base operations.</param>
        /// <param name="permissionRepository">Repository for permission operations.</param>
        /// <param name="logger">Logger for this service.</param>
        public KnowledgeBaseAuthorizationService(
            IKnowledgeBaseRepository knowledgeBaseRepository,
            IKnowledgeBasePermissionRepository permissionRepository,
            ILogger<KnowledgeBaseAuthorizationService> logger)
        {
            _knowledgeBaseRepository = knowledgeBaseRepository ?? throw new ArgumentNullException(nameof(knowledgeBaseRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> HasPermissionAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType requiredPermission)
        {
            try
            {
                return await _permissionRepository.HasPermissionAsync(userId, knowledgeBaseId, requiredPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission for user {UserId} on knowledge base {KnowledgeBaseId}", userId, knowledgeBaseId);
                return false; // Fail closed - deny access on error
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeBasePermissionType?> GetEffectivePermissionAsync(Guid userId, Guid knowledgeBaseId)
        {
            try
            {
                // Check if user is owner (always gets admin permissions)
                if (await _knowledgeBaseRepository.IsOwnerAsync(knowledgeBaseId, userId))
                {
                    return KnowledgeBasePermissionType.Admin;
                }

                // Check if knowledge base exists and is public
                Models.KnowledgeBase? knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
                if (knowledgeBase == null)
                {
                    return null; // Knowledge base doesn't exist
                }

                // Check explicit permission
                KnowledgeBasePermissionType? explicitPermission = await _permissionRepository.GetUserPermissionAsync(userId, knowledgeBaseId);
                if (explicitPermission.HasValue)
                {
                    return explicitPermission.Value;
                }

                // Check if public (read-only access)
                if (knowledgeBase.IsPublic)
                {
                    return KnowledgeBasePermissionType.Read;
                }

                return null; // No access
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting effective permission for user {UserId} on knowledge base {KnowledgeBaseId}", userId, knowledgeBaseId);
                return null; // Fail closed - deny access on error
            }
        }

        /// <inheritdoc/>
        public async Task<List<Guid>> GetAccessibleKnowledgeBaseIdsAsync(Guid userId, KnowledgeBasePermissionType minimumPermission = KnowledgeBasePermissionType.Read)
        {
            try
            {
                return await _permissionRepository.GetAccessibleKnowledgeBaseIdsAsync(userId, minimumPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible knowledge bases for user {UserId} with minimum permission {MinimumPermission}", userId, minimumPermission);
                return new List<Guid>(); // Return empty list on error
            }
        }

        /// <inheritdoc/>
        public async Task ValidateAccessAsync(Guid userId, Guid knowledgeBaseId, KnowledgeBasePermissionType requiredPermission, string actionDescription)
        {
            bool hasPermission = await HasPermissionAsync(userId, knowledgeBaseId, requiredPermission);
            if (!hasPermission)
            {
                _logger.LogWarning("Access denied: User {UserId} attempted to {ActionDescription} on knowledge base {KnowledgeBaseId} but lacks {RequiredPermission} permission", 
                    userId, actionDescription, knowledgeBaseId, requiredPermission);
                
                throw new UnauthorizedAccessException($"Access denied. {requiredPermission} permission required to {actionDescription}.");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsOwnerAsync(Guid userId, Guid knowledgeBaseId)
        {
            try
            {
                return await _knowledgeBaseRepository.IsOwnerAsync(knowledgeBaseId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ownership for user {UserId} on knowledge base {KnowledgeBaseId}", userId, knowledgeBaseId);
                return false; // Fail closed
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsPublicAsync(Guid knowledgeBaseId)
        {
            try
            {
                Models.KnowledgeBase? knowledgeBase = await _knowledgeBaseRepository.GetByIdAsync(knowledgeBaseId);
                return knowledgeBase?.IsPublic ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if knowledge base {KnowledgeBaseId} is public", knowledgeBaseId);
                return false; // Fail closed
            }
        }
    }
}

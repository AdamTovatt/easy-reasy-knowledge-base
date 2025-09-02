namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a permission granted to a user for a specific library.
    /// </summary>
    public class LibraryPermission
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryPermission"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the permission record.</param>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="userId">The unique identifier of the user granted the permission.</param>
        /// <param name="permissionType">The type of permission granted.</param>
        /// <param name="grantedByUserId">The unique identifier of the user who granted the permission.</param>
        /// <param name="createdAt">The timestamp when the permission was granted.</param>
        public LibraryPermission(
            Guid id,
            Guid libraryId,
            Guid userId,
            LibraryPermissionType permissionType,
            Guid grantedByUserId,
            DateTime createdAt)
        {
            Id = id;
            LibraryId = libraryId;
            UserId = userId;
            PermissionType = permissionType;
            GrantedByUserId = grantedByUserId;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Gets the unique identifier for the permission record.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the unique identifier of the library.
        /// </summary>
        public Guid LibraryId { get; }

        /// <summary>
        /// Gets the unique identifier of the user granted the permission.
        /// </summary>
        public Guid UserId { get; }

        /// <summary>
        /// Gets the type of permission granted.
        /// </summary>
        public LibraryPermissionType PermissionType { get; }

        /// <summary>
        /// Gets the unique identifier of the user who granted the permission.
        /// </summary>
        public Guid GrantedByUserId { get; }

        /// <summary>
        /// Gets the timestamp when the permission was granted.
        /// </summary>
        public DateTime CreatedAt { get; }
    }
}

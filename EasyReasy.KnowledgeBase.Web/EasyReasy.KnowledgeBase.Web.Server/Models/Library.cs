namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a library in the system.
    /// </summary>
    public class Library
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Library"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the library.</param>
        /// <param name="name">The name of the library.</param>
        /// <param name="description">The description of the library.</param>
        /// <param name="ownerId">The unique identifier of the user who owns the library.</param>
        /// <param name="isPublic">Whether the library is publicly readable.</param>
        /// <param name="createdAt">The timestamp when the library was created.</param>
        /// <param name="updatedAt">The timestamp when the library was last updated.</param>
        public Library(
            Guid id,
            string name,
            string? description,
            Guid ownerId,
            bool isPublic,
            DateTime createdAt,
            DateTime updatedAt)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            OwnerId = ownerId;
            IsPublic = isPublic;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Gets the unique identifier for the library.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the library.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the library.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the unique identifier of the user who owns the library.
        /// </summary>
        public Guid OwnerId { get; }

        /// <summary>
        /// Gets whether the library is publicly readable.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets the timestamp when the library was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the library was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; }
    }
}

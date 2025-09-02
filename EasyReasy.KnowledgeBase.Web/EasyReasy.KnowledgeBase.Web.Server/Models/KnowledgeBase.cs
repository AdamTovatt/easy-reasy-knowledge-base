namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a knowledge base in the system.
    /// </summary>
    public class KnowledgeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeBase"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the knowledge base.</param>
        /// <param name="name">The name of the knowledge base.</param>
        /// <param name="description">The description of the knowledge base.</param>
        /// <param name="ownerId">The unique identifier of the user who owns the knowledge base.</param>
        /// <param name="isPublic">Whether the knowledge base is publicly readable.</param>
        /// <param name="createdAt">The timestamp when the knowledge base was created.</param>
        /// <param name="updatedAt">The timestamp when the knowledge base was last updated.</param>
        public KnowledgeBase(
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
        /// Gets the unique identifier for the knowledge base.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the knowledge base.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the knowledge base.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the unique identifier of the user who owns the knowledge base.
        /// </summary>
        public Guid OwnerId { get; }

        /// <summary>
        /// Gets whether the knowledge base is publicly readable.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets the timestamp when the knowledge base was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the knowledge base was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; }
    }
}

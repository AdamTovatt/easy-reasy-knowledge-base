namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a user in the system with authentication and role information.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Gets or sets the user's email address, which serves as the login identifier for authentication.
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Gets or sets the hashed password for the user.
        /// </summary>
        public string PasswordHash { get; set; }
        
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; }
        
        /// <summary>
        /// Gets or sets whether the user account is active and can be used for authentication.
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the user's last successful login, or null if they have never logged in.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the user account was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the list of role names assigned to the user. This is populated from the user_role table.
        /// </summary>
        public List<string> Roles { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class with all user information.
        /// </summary>
        /// <param name="id">The unique identifier for the user.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="passwordHash">The hashed password for the user.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="isActive">Whether the user account is active.</param>
        /// <param name="lastLoginAt">The timestamp of the user's last successful login.</param>
        /// <param name="createdAt">The timestamp when the user account was created.</param>
        /// <param name="updatedAt">The timestamp when the user account was last updated.</param>
        /// <param name="roles">The list of role names assigned to the user.</param>
        public User(
            Guid id,
            string email,
            string passwordHash,
            string firstName,
            string lastName,
            bool isActive,
            DateTime? lastLoginAt,
            DateTime createdAt,
            DateTime updatedAt,
            List<string> roles)
        {
            Id = id;
            Email = email;
            PasswordHash = passwordHash;
            FirstName = firstName;
            LastName = lastName;
            IsActive = isActive;
            LastLoginAt = lastLoginAt;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Roles = roles;
        }
    }
}

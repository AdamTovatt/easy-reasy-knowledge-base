namespace EasyReasy.KnowledgeBase.Web.Server.Models.Dto
{
    /// <summary>
    /// Represents user information returned by the API, excluding sensitive data like password hash.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets whether the user account is active.
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
        /// Gets or sets the list of role names assigned to the user.
        /// </summary>
        public List<string> Roles { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDto"/> class with the specified user information.
        /// </summary>
        /// <param name="id">The unique identifier for the user.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="isActive">Whether the user account is active.</param>
        /// <param name="lastLoginAt">The timestamp of the user's last successful login.</param>
        /// <param name="createdAt">The timestamp when the user account was created.</param>
        /// <param name="updatedAt">The timestamp when the user account was last updated.</param>
        /// <param name="roles">The list of role names assigned to the user.</param>
        public UserDto(
            Guid id,
            string email,
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
            FirstName = firstName;
            LastName = lastName;
            IsActive = isActive;
            LastLoginAt = lastLoginAt;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Roles = roles;
        }

        /// <summary>
        /// Creates a <see cref="UserDto"/> from a <see cref="User"/> model, excluding sensitive data.
        /// </summary>
        /// <param name="user">The user model to convert.</param>
        /// <returns>A new <see cref="UserDto"/> containing the user information.</returns>
        public static UserDto FromUser(User user)
        {
            return new UserDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt,
                user.Roles);
        }
    }
}

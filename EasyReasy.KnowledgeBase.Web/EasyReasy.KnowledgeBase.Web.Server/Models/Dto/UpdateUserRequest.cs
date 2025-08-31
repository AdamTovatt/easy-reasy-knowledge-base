using System.ComponentModel.DataAnnotations;

namespace EasyReasy.KnowledgeBase.Web.Server.Models.Dto
{
    /// <summary>
    /// Represents a request to update an existing user account.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Gets or sets the user's email address, which will serve as the login identifier for authentication.
        /// </summary>
        [EmailAddress]
        [MaxLength(254)] // RFC 5321 max length
        public string? Email { get; set; }
        
        /// <summary>
        /// Gets or sets the user's new password. This will be hashed before storage. Set to null to keep the existing password.
        /// </summary>
        [MinLength(8)]
        [MaxLength(128)]
        public string? Password { get; set; }
        
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [MaxLength(100)]
        public string? FirstName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [MaxLength(100)]
        public string? LastName { get; set; }
        
        /// <summary>
        /// Gets or sets whether the user account is active and can be used for authentication.
        /// </summary>
        public bool? IsActive { get; set; }
        
        /// <summary>
        /// Gets or sets the list of role names to assign to the user. Set to null to keep existing roles.
        /// </summary>
        public List<string>? Roles { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateUserRequest"/> class.
        /// </summary>
        public UpdateUserRequest()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateUserRequest"/> class with the specified user information.
        /// </summary>
        /// <param name="email">The user's email address, or null to keep existing.</param>
        /// <param name="password">The user's new password, or null to keep existing.</param>
        /// <param name="firstName">The user's first name, or null to keep existing.</param>
        /// <param name="lastName">The user's last name, or null to keep existing.</param>
        /// <param name="isActive">Whether the user account is active, or null to keep existing.</param>
        /// <param name="roles">The list of role names, or null to keep existing.</param>
        public UpdateUserRequest(
            string? email = null,
            string? password = null,
            string? firstName = null,
            string? lastName = null,
            bool? isActive = null,
            List<string>? roles = null)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            IsActive = isActive;
            Roles = roles;
        }
    }
}

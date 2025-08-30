using System.ComponentModel.DataAnnotations;

namespace EasyReasy.KnowledgeBase.Web.Server.Models.Dto
{
    /// <summary>
    /// Represents a request to create a new user account.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Gets or sets the user's email address, which will serve as the username for authentication.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(254)] // RFC 5321 max length
        public string Email { get; set; }
        
        /// <summary>
        /// Gets or sets the user's password. This will be hashed before storage.
        /// </summary>
        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string Password { get; set; }
        
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }
        
        /// <summary>
        /// Gets or sets the list of role names to assign to the user.
        /// </summary>
        public List<string> Roles { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUserRequest"/> class with the specified user information.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="roles">The list of role names to assign to the user.</param>
        public CreateUserRequest(
            string email,
            string password,
            string firstName,
            string lastName,
            List<string> roles)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            Roles = roles;
        }
    }
}

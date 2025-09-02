namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents the types of permissions that can be granted for a library.
    /// </summary>
    public enum LibraryPermissionType
    {
        /// <summary>
        /// Read-only access to view files and download content.
        /// </summary>
        Read,

        /// <summary>
        /// Write access to upload and delete own files, includes read permissions.
        /// </summary>
        Write,

        /// <summary>
        /// Administrative access to manage permissions and delete any files, includes read/write permissions.
        /// </summary>
        Admin
    }
}

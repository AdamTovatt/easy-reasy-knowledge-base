namespace EasyReasy.KnowledgeBase.Web.Server.Models.Storage
{
    /// <summary>
    /// Request model for file upload operations.
    /// </summary>
    public class FileUploadRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadRequest"/> class.
        /// </summary>
        /// <param name="libraryId">The unique identifier for the library.</param>
        /// <param name="file">The file to upload.</param>
        public FileUploadRequest(Guid libraryId, IFormFile file)
        {
            LibraryId = libraryId;
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>
        /// Gets the unique identifier for the library.
        /// </summary>
        public Guid LibraryId { get; }

        /// <summary>
        /// Gets the file to upload.
        /// </summary>
        public IFormFile File { get; }
    }
}

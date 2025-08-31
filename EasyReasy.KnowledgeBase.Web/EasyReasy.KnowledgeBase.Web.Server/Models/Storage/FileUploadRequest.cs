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
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="file">The file to upload.</param>
        public FileUploadRequest(Guid knowledgeBaseId, IFormFile file)
        {
            KnowledgeBaseId = knowledgeBaseId;
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>
        /// Gets the unique identifier for the knowledge base.
        /// </summary>
        public Guid KnowledgeBaseId { get; }

        /// <summary>
        /// Gets the file to upload.
        /// </summary>
        public IFormFile File { get; }
    }
}

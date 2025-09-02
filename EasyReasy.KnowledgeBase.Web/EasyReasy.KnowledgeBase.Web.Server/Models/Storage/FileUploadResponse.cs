using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;

namespace EasyReasy.KnowledgeBase.Web.Server.Models.Storage
{
    /// <summary>
    /// Response model for file upload operations.
    /// </summary>
    public class FileUploadResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadResponse"/> class.
        /// </summary>
        /// <param name="success">Whether the upload was successful.</param>
        /// <param name="message">A message describing the result.</param>
        /// <param name="fileInfo">Information about the uploaded file, if successful.</param>
        public FileUploadResponse(bool success, string message, KnowledgeFileDto? fileInfo = null)
        {
            Success = success;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            FileInfo = fileInfo;
        }

        /// <summary>
        /// Gets a value indicating whether the upload was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets a message describing the result of the upload operation.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets information about the uploaded file, if the upload was successful.
        /// </summary>
        public KnowledgeFileDto? FileInfo { get; }
    }
}

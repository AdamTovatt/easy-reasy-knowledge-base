namespace EasyReasy.KnowledgeBase.Web.Server.Models.Storage
{
    /// <summary>
    /// Request model for initiating a chunked upload session.
    /// </summary>
    public class InitiateChunkedUploadRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitiateChunkedUploadRequest"/> class.
        /// </summary>
        /// <param name="libraryId">The unique identifier for the library.</param>
        /// <param name="fileName">The name of the file to upload.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="totalSize">The total size of the file in bytes.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        public InitiateChunkedUploadRequest(
            Guid libraryId,
            string fileName,
            string contentType,
            long totalSize,
            int chunkSize)
        {
            LibraryId = libraryId;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            TotalSize = totalSize;
            ChunkSize = chunkSize;
        }

        /// <summary>
        /// Gets the unique identifier for the library.
        /// </summary>
        public Guid LibraryId { get; }

        /// <summary>
        /// Gets the name of the file to upload.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the MIME type of the file.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the total size of the file in bytes.
        /// </summary>
        public long TotalSize { get; }

        /// <summary>
        /// Gets the size of each chunk in bytes.
        /// </summary>
        public int ChunkSize { get; }
    }
}

namespace EasyReasy.KnowledgeBase.Web.Server.Models.Storage
{
    /// <summary>
    /// Represents an active chunked upload session.
    /// </summary>
    public class ChunkedUploadSession
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkedUploadSession"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="libraryId">The unique identifier for the library.</param>
        /// <param name="originalFileName">The original name of the file being uploaded.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="totalSize">The total size of the file in bytes.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user uploading the file.</param>
        /// <param name="createdAt">The timestamp when the session was created.</param>
        /// <param name="expiresAt">The timestamp when the session expires.</param>
        public ChunkedUploadSession(
            Guid sessionId,
            Guid libraryId,
            string originalFileName,
            string contentType,
            long totalSize,
            int chunkSize,
            Guid uploadedByUserId,
            DateTime createdAt,
            DateTime expiresAt)
        {
            SessionId = sessionId;
            LibraryId = libraryId;
            OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            TotalSize = totalSize;
            ChunkSize = chunkSize;
            UploadedByUserId = uploadedByUserId;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            UploadedChunks = new List<int>();
            TempFilePath = string.Empty; // Will be set when session is created
        }

        /// <summary>
        /// Gets the unique identifier for the upload session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the unique identifier for the library.
        /// </summary>
        public Guid LibraryId { get; }

        /// <summary>
        /// Gets the original name of the file being uploaded.
        /// </summary>
        public string OriginalFileName { get; }

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

        /// <summary>
        /// Gets the unique identifier of the user uploading the file.
        /// </summary>
        public Guid UploadedByUserId { get; }

        /// <summary>
        /// Gets the timestamp when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the timestamp when the session expires.
        /// </summary>
        public DateTime ExpiresAt { get; }

        /// <summary>
        /// Gets the temporary file path where chunks are being assembled.
        /// </summary>
        public string TempFilePath { get; set; }

        /// <summary>
        /// Gets the list of chunk numbers that have been successfully uploaded.
        /// </summary>
        public List<int> UploadedChunks { get; }

        /// <summary>
        /// Gets the total number of chunks expected for this upload.
        /// </summary>
        public int TotalChunks => (int)Math.Ceiling((double)TotalSize / ChunkSize);

        /// <summary>
        /// Gets a value indicating whether all chunks have been uploaded.
        /// </summary>
        public bool IsComplete => UploadedChunks.Count == TotalChunks &&
                                  UploadedChunks.All(chunk => chunk >= 0 && chunk < TotalChunks);

        /// <summary>
        /// Gets the percentage of upload completion.
        /// </summary>
        public double ProgressPercentage => TotalChunks == 0 ? 0 : (double)UploadedChunks.Count / TotalChunks * 100;

        /// <summary>
        /// Gets a value indicating whether the session has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}

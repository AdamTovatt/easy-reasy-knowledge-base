namespace EasyReasy.KnowledgeBase.Web.Server.Models.Storage
{
    /// <summary>
    /// Response model for chunked upload operations.
    /// </summary>
    public class ChunkedUploadResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkedUploadResponse"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the upload session.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="totalChunks">The total number of chunks expected.</param>
        /// <param name="uploadedChunks">The list of chunk numbers that have been successfully uploaded.</param>
        /// <param name="isComplete">Whether all chunks have been uploaded.</param>
        /// <param name="progressPercentage">The percentage of upload completion.</param>
        /// <param name="expiresAt">The timestamp when the session expires.</param>
        public ChunkedUploadResponse(
            Guid sessionId,
            int chunkSize,
            int totalChunks,
            List<int> uploadedChunks,
            bool isComplete,
            double progressPercentage,
            DateTime expiresAt)
        {
            SessionId = sessionId;
            ChunkSize = chunkSize;
            TotalChunks = totalChunks;
            UploadedChunks = uploadedChunks ?? throw new ArgumentNullException(nameof(uploadedChunks));
            IsComplete = isComplete;
            ProgressPercentage = progressPercentage;
            ExpiresAt = expiresAt;
        }

        /// <summary>
        /// Gets the unique identifier for the upload session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the size of each chunk in bytes.
        /// </summary>
        public int ChunkSize { get; }

        /// <summary>
        /// Gets the total number of chunks expected.
        /// </summary>
        public int TotalChunks { get; }

        /// <summary>
        /// Gets the list of chunk numbers that have been successfully uploaded.
        /// </summary>
        public List<int> UploadedChunks { get; }

        /// <summary>
        /// Gets a value indicating whether all chunks have been uploaded.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Gets the percentage of upload completion.
        /// </summary>
        public double ProgressPercentage { get; }

        /// <summary>
        /// Gets the timestamp when the session expires.
        /// </summary>
        public DateTime ExpiresAt { get; }

        /// <summary>
        /// Creates a response from a chunked upload session.
        /// </summary>
        /// <param name="session">The upload session.</param>
        /// <returns>A chunked upload response.</returns>
        public static ChunkedUploadResponse FromSession(ChunkedUploadSession session)
        {
            return new ChunkedUploadResponse(
                sessionId: session.SessionId,
                chunkSize: session.ChunkSize,
                totalChunks: session.TotalChunks,
                uploadedChunks: new List<int>(session.UploadedChunks),
                isComplete: session.IsComplete,
                progressPercentage: session.ProgressPercentage,
                expiresAt: session.ExpiresAt
            );
        }
    }
}

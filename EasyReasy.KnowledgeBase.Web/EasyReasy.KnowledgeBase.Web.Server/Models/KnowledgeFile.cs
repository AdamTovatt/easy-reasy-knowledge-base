namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a file stored in the knowledge base system.
    /// </summary>
    public class KnowledgeFile
    {
        /// <summary>
        /// Gets or sets the unique identifier for the file.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier for the knowledge base this file belongs to.
        /// </summary>
        public Guid KnowledgeBaseId { get; set; }
        
        /// <summary>
        /// Gets or sets the original name of the file when it was uploaded.
        /// </summary>
        public string OriginalFileName { get; set; }
        
        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        public string ContentType { get; set; }
        
        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long SizeInBytes { get; set; }
        
        /// <summary>
        /// Gets or sets the relative path to the file within the file storage system.
        /// </summary>
        public string RelativePath { get; set; }
        
        /// <summary>
        /// Gets or sets the SHA-256 hash of the file content.
        /// </summary>
        public byte[] Hash { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier of the user who uploaded the file.
        /// </summary>
        public Guid UploadedByUserId { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the file was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the file record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the file record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeFile"/> class with all file information.
        /// </summary>
        /// <param name="id">The unique identifier for the file.</param>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="originalFileName">The original name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="sizeInBytes">The size of the file in bytes.</param>
        /// <param name="relativePath">The relative path to the file.</param>
        /// <param name="hash">The SHA-256 hash of the file content.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user who uploaded the file.</param>
        /// <param name="uploadedAt">The timestamp when the file was uploaded.</param>
        /// <param name="createdAt">The timestamp when the file record was created.</param>
        /// <param name="updatedAt">The timestamp when the file record was last updated.</param>
        public KnowledgeFile(
            Guid id,
            Guid knowledgeBaseId,
            string originalFileName,
            string contentType,
            long sizeInBytes,
            string relativePath,
            byte[] hash,
            Guid uploadedByUserId,
            DateTime uploadedAt,
            DateTime createdAt,
            DateTime updatedAt)
        {
            Id = id;
            KnowledgeBaseId = knowledgeBaseId;
            OriginalFileName = originalFileName;
            ContentType = contentType;
            SizeInBytes = sizeInBytes;
            RelativePath = relativePath;
            Hash = hash;
            UploadedByUserId = uploadedByUserId;
            UploadedAt = uploadedAt;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        /// <summary>
        /// Gets a user-friendly display of the file size.
        /// </summary>
        public string FormattedSize => FormatFileSize(SizeInBytes);

        private static string FormatFileSize(long sizeInBytes)
        {
            if (sizeInBytes == 0)
                return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = sizeInBytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
    }
}

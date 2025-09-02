namespace EasyReasy.KnowledgeBase.Web.Server.Models.Dto
{
    /// <summary>
    /// Information about a file stored in the knowledge base system.
    /// </summary>
    public class KnowledgeFileDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeFileDto"/> class.
        /// </summary>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="knowledgeBaseId">The unique identifier for the knowledge base.</param>
        /// <param name="originalFileName">The original name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="sizeInBytes">The size of the file in bytes.</param>
        /// <param name="uploadedAt">The date and time when the file was uploaded.</param>
        /// <param name="relativePath">The relative path to the file within the knowledge base.</param>
        /// <param name="hashHex">The SHA-256 hash of the file content as a hex string.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user who uploaded the file.</param>
        public KnowledgeFileDto(
            Guid fileId,
            Guid knowledgeBaseId,
            string originalFileName,
            string contentType,
            long sizeInBytes,
            DateTime uploadedAt,
            string relativePath,
            string hashHex,
            Guid uploadedByUserId)
        {
            FileId = fileId;
            KnowledgeBaseId = knowledgeBaseId;
            OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            SizeInBytes = sizeInBytes;
            UploadedAt = uploadedAt;
            RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            HashHex = hashHex ?? throw new ArgumentNullException(nameof(hashHex));
            UploadedByUserId = uploadedByUserId;
        }

        /// <summary>
        /// Gets the unique identifier for the file.
        /// </summary>
        public Guid FileId { get; }

        /// <summary>
        /// Gets the unique identifier for the knowledge base.
        /// </summary>
        public Guid KnowledgeBaseId { get; }

        /// <summary>
        /// Gets the original name of the file when it was uploaded.
        /// </summary>
        public string OriginalFileName { get; }

        /// <summary>
        /// Gets the MIME type of the file.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public long SizeInBytes { get; }

        /// <summary>
        /// Gets the date and time when the file was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; }

        /// <summary>
        /// Gets the relative path to the file within the knowledge base.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Gets the SHA-256 hash of the file content as a hex string.
        /// </summary>
        public string HashHex { get; }

        /// <summary>
        /// Gets the unique identifier of the user who uploaded the file.
        /// </summary>
        public Guid UploadedByUserId { get; }

        /// <summary>
        /// Gets a user-friendly display of the file size.
        /// </summary>
        public string FormattedSize => FormatFileSize(SizeInBytes);

        /// <summary>
        /// Creates a KnowledgeFileDto instance from a KnowledgeFile model.
        /// </summary>
        /// <param name="file">The knowledge file model to convert.</param>
        /// <returns>A KnowledgeFileDto instance.</returns>
        public static KnowledgeFileDto FromFile(KnowledgeFile file)
        {
            return new KnowledgeFileDto(
                fileId: file.Id,
                knowledgeBaseId: file.KnowledgeBaseId,
                originalFileName: file.OriginalFileName,
                contentType: file.ContentType,
                sizeInBytes: file.SizeInBytes,
                uploadedAt: file.UploadedAt,
                relativePath: file.RelativePath,
                hashHex: Convert.ToHexString(file.Hash),
                uploadedByUserId: file.UploadedByUserId
            );
        }

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

using EasyReasy.KnowledgeBase.Web.Server.ExtensionMethods;

namespace EasyReasy.KnowledgeBase.Web.Server.Models.Dto
{
    /// <summary>
    /// Information about a file stored in the library system.
    /// </summary>
    public class LibraryFileDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryFileDto"/> class.
        /// </summary>
        /// <param name="fileId">The unique identifier for the file.</param>
        /// <param name="libraryId">The unique identifier for the library.</param>
        /// <param name="originalFileName">The original name of the file.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="sizeInBytes">The size of the file in bytes.</param>
        /// <param name="uploadedAt">The date and time when the file was uploaded.</param>
        /// <param name="relativePath">The relative path to the file within the library.</param>
        /// <param name="hashHex">The SHA-256 hash of the file content as a hex string.</param>
        /// <param name="uploadedByUserId">The unique identifier of the user who uploaded the file.</param>
        public LibraryFileDto(
            Guid fileId,
            Guid libraryId,
            string originalFileName,
            string contentType,
            long sizeInBytes,
            DateTime uploadedAt,
            string relativePath,
            string hashHex,
            Guid uploadedByUserId)
        {
            FileId = fileId;
            LibraryId = libraryId;
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
        /// Gets the unique identifier for the library.
        /// </summary>
        public Guid LibraryId { get; }

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
        /// Gets the relative path to the file within the library.
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
        public string FormattedSize => SizeInBytes.ToFileSizeString();

        /// <summary>
        /// Creates a LibraryFileDto instance from a LibraryFile model.
        /// </summary>
        /// <param name="file">The library file model to convert.</param>
        /// <returns>A LibraryFileDto instance.</returns>
        public static LibraryFileDto FromFile(LibraryFile file)
        {
            return new LibraryFileDto(
                fileId: file.Id,
                libraryId: file.LibraryId,
                originalFileName: file.OriginalFileName,
                contentType: file.ContentType,
                sizeInBytes: file.SizeInBytes,
                uploadedAt: file.UploadedAt,
                relativePath: file.RelativePath,
                hashHex: Convert.ToHexString(file.Hash),
                uploadedByUserId: file.UploadedByUserId
            );
        }
    }
}

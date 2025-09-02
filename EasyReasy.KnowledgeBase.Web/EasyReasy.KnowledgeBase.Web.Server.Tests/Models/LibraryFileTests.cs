using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Models
{
    /// <summary>
    /// Unit tests for the LibraryFile model.
    /// </summary>
    [TestClass]
    public class LibraryFileTests : GeneralTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            LoadTestEnvironmentVariables();
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithValidData_CreatesInstance()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            string originalFileName = "test-document.pdf";
            string contentType = "application/pdf";
            long sizeInBytes = 1024768;
            string relativePath = "lib_abc123/test-document.pdf";
            byte[] hash = CreateTestHash();
            Guid uploadedByUserId = Guid.NewGuid();
            DateTime uploadedAt = DateTime.UtcNow.AddMinutes(-5);
            DateTime createdAt = DateTime.UtcNow.AddMinutes(-3);
            DateTime updatedAt = DateTime.UtcNow.AddMinutes(-1);

            // Act
            LibraryFile libraryFile = new LibraryFile(
                id, libraryId, originalFileName, contentType, sizeInBytes,
                relativePath, hash, uploadedByUserId, uploadedAt, createdAt, updatedAt);

            // Assert
            Assert.AreEqual(id, libraryFile.Id);
            Assert.AreEqual(libraryId, libraryFile.LibraryId);
            Assert.AreEqual(originalFileName, libraryFile.OriginalFileName);
            Assert.AreEqual(contentType, libraryFile.ContentType);
            Assert.AreEqual(sizeInBytes, libraryFile.SizeInBytes);
            Assert.AreEqual(relativePath, libraryFile.RelativePath);
            CollectionAssert.AreEqual(hash, libraryFile.Hash);
            Assert.AreEqual(uploadedByUserId, libraryFile.UploadedByUserId);
            Assert.AreEqual(uploadedAt, libraryFile.UploadedAt);
            Assert.AreEqual(createdAt, libraryFile.CreatedAt);
            Assert.AreEqual(updatedAt, libraryFile.UpdatedAt);
        }

        [TestMethod]
        public void Constructor_WithEmptyGuids_CreatesInstance()
        {
            // Arrange
            Guid emptyId = Guid.Empty;
            Guid emptyLibraryId = Guid.Empty;
            Guid emptyUploadedByUserId = Guid.Empty;

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                id: emptyId,
                libraryId: emptyLibraryId,
                uploadedByUserId: emptyUploadedByUserId);

            // Assert
            Assert.AreEqual(emptyId, libraryFile.Id);
            Assert.AreEqual(emptyLibraryId, libraryFile.LibraryId);
            Assert.AreEqual(emptyUploadedByUserId, libraryFile.UploadedByUserId);
        }

        [TestMethod]
        public void Constructor_WithNullStrings_CreatesInstance()
        {
            // Arrange & Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                originalFileName: null!,
                contentType: null!,
                relativePath: null!);

            // Assert - Model allows null strings (no validation in constructor)
            Assert.IsNull(libraryFile.OriginalFileName);
            Assert.IsNull(libraryFile.ContentType);
            Assert.IsNull(libraryFile.RelativePath);
        }

        [TestMethod]
        public void Constructor_WithEmptyStrings_CreatesInstance()
        {
            // Arrange & Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                originalFileName: "",
                contentType: "",
                relativePath: "");

            // Assert
            Assert.AreEqual("", libraryFile.OriginalFileName);
            Assert.AreEqual("", libraryFile.ContentType);
            Assert.AreEqual("", libraryFile.RelativePath);
        }

        [TestMethod]
        public void Constructor_WithNullHash_CreatesInstance()
        {
            // Arrange & Act - Call constructor directly to ensure null hash is preserved
            LibraryFile libraryFile = new LibraryFile(
                id: Guid.NewGuid(),
                libraryId: Guid.NewGuid(),
                originalFileName: "test.txt",
                contentType: "text/plain",
                sizeInBytes: 1024,
                relativePath: "lib_test/test.txt",
                hash: null!,
                uploadedByUserId: Guid.NewGuid(),
                uploadedAt: DateTime.UtcNow.AddMinutes(-10),
                createdAt: DateTime.UtcNow.AddMinutes(-8),
                updatedAt: DateTime.UtcNow.AddMinutes(-5)
            );

            // Assert - Model allows null hash (no validation in constructor)
            Assert.IsNull(libraryFile.Hash);
        }

        [TestMethod]
        public void Constructor_WithEmptyHash_CreatesInstance()
        {
            // Arrange
            byte[] emptyHash = Array.Empty<byte>();

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(hash: emptyHash);

            // Assert
            Assert.IsNotNull(libraryFile.Hash);
            Assert.AreEqual(0, libraryFile.Hash.Length);
        }

        [TestMethod]
        public void Constructor_WithNegativeSize_CreatesInstance()
        {
            // Arrange & Act
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: -1);

            // Assert - Model allows negative sizes (no validation in constructor)
            Assert.AreEqual(-1, libraryFile.SizeInBytes);
        }

        [TestMethod]
        public void Constructor_WithZeroSize_CreatesInstance()
        {
            // Arrange & Act
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 0);

            // Assert
            Assert.AreEqual(0, libraryFile.SizeInBytes);
        }

        [TestMethod]
        public void Constructor_WithMaxSize_CreatesInstance()
        {
            // Arrange
            long maxSize = long.MaxValue;

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: maxSize);

            // Assert
            Assert.AreEqual(maxSize, libraryFile.SizeInBytes);
        }

        #endregion

        #region FormattedSize Property Tests

        [TestMethod]
        public void FormattedSize_WithZeroBytes_ReturnsCorrectFormat()
        {
            // Arrange
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 0);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("0 B", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithBytesOnly_ReturnsCorrectFormat()
        {
            // Arrange
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 512);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("512 B", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithKilobytes_ReturnsCorrectFormat()
        {
            // Arrange - 1.5 KB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 1536);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("1.5 KB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithMegabytes_ReturnsCorrectFormat()
        {
            // Arrange - 2.25 MB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 2359296);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("2.25 MB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithGigabytes_ReturnsCorrectFormat()
        {
            // Arrange - 1.5 GB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 1610612736);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("1.5 GB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithTerabytes_ReturnsCorrectFormat()
        {
            // Arrange - 2.5 TB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 2748779069440);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("2.5 TB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithExactKilobyte_ReturnsCorrectFormat()
        {
            // Arrange - Exactly 1 KB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 1024);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("1 KB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithExactMegabyte_ReturnsCorrectFormat()
        {
            // Arrange - Exactly 1 MB
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 1048576);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("1 MB", formattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithVerySmallDecimal_ReturnsCorrectFormat()
        {
            // Arrange - Size that results in very small decimal
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 1025);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.AreEqual("1 KB", formattedSize); // Should round down to 1 KB
        }

        [TestMethod]
        public void FormattedSize_WithLargeTerabytes_StaysInTerabytes()
        {
            // Arrange - Very large TB value
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: 5497558138880); // ~5 TB

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert
            Assert.IsTrue(formattedSize.EndsWith(" TB"));
            Assert.IsTrue(formattedSize.StartsWith("5"));
        }

        [TestMethod]
        public void FormattedSize_WithNegativeSize_HandlesGracefully()
        {
            // Arrange
            LibraryFile libraryFile = CreateTestLibraryFile(sizeInBytes: -1024);

            // Act
            string formattedSize = libraryFile.FormattedSize;

            // Assert - Behavior with negative size (implementation dependent)
            Assert.IsNotNull(formattedSize);
            Assert.IsTrue(formattedSize.Contains("-") || formattedSize.Contains("0"));
        }

        #endregion

        #region Hash Property Tests

        [TestMethod]
        public void Hash_WithValidSha256Hash_StoresCorrectly()
        {
            // Arrange
            byte[] testHash = CreateTestSha256Hash();

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(hash: testHash);

            // Assert
            CollectionAssert.AreEqual(testHash, libraryFile.Hash);
            Assert.AreEqual(32, libraryFile.Hash.Length); // SHA-256 is 32 bytes
        }

        [TestMethod]
        public void Hash_WithDifferentLengthHash_StoresCorrectly()
        {
            // Arrange - Non-standard hash length
            byte[] customHash = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(hash: customHash);

            // Assert
            CollectionAssert.AreEqual(customHash, libraryFile.Hash);
            Assert.AreEqual(5, libraryFile.Hash.Length);
        }

        #endregion

        #region Date Property Tests

        [TestMethod]
        public void DateProperties_WithVariousDates_StoreCorrectly()
        {
            // Arrange
            DateTime uploadedAt = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
            DateTime createdAt = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
            DateTime updatedAt = new DateTime(2024, 1, 15, 11, 30, 15, DateTimeKind.Utc);

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                uploadedAt: uploadedAt,
                createdAt: createdAt,
                updatedAt: updatedAt);

            // Assert
            Assert.AreEqual(uploadedAt, libraryFile.UploadedAt);
            Assert.AreEqual(createdAt, libraryFile.CreatedAt);
            Assert.AreEqual(updatedAt, libraryFile.UpdatedAt);
        }

        [TestMethod]
        public void DateProperties_WithMinDateTime_StoresCorrectly()
        {
            // Arrange
            DateTime minDate = DateTime.MinValue;

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                uploadedAt: minDate,
                createdAt: minDate,
                updatedAt: minDate);

            // Assert
            Assert.AreEqual(minDate, libraryFile.UploadedAt);
            Assert.AreEqual(minDate, libraryFile.CreatedAt);
            Assert.AreEqual(minDate, libraryFile.UpdatedAt);
        }

        [TestMethod]
        public void DateProperties_WithMaxDateTime_StoresCorrectly()
        {
            // Arrange
            DateTime maxDate = DateTime.MaxValue;

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                uploadedAt: maxDate,
                createdAt: maxDate,
                updatedAt: maxDate);

            // Assert
            Assert.AreEqual(maxDate, libraryFile.UploadedAt);
            Assert.AreEqual(maxDate, libraryFile.CreatedAt);
            Assert.AreEqual(maxDate, libraryFile.UpdatedAt);
        }

        #endregion

        #region File Type Tests

        [TestMethod]
        public void ContentType_WithCommonMimeTypes_StoresCorrectly()
        {
            // Arrange & Act
            (string, string)[] testCases = new[]
            {
                ("document.pdf", "application/pdf"),
                ("image.jpg", "image/jpeg"),
                ("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
                ("data.json", "application/json"),
                ("style.css", "text/css"),
                ("script.js", "application/javascript"),
                ("archive.zip", "application/zip"),
                ("plain.txt", "text/plain")
            };

            foreach ((string fileName, string expectedContentType) in testCases)
            {
                LibraryFile libraryFile = CreateTestLibraryFile(
                    originalFileName: fileName,
                    contentType: expectedContentType);

                // Assert
                Assert.AreEqual(expectedContentType, libraryFile.ContentType,
                    $"Content type mismatch for file: {fileName}");
                Assert.AreEqual(fileName, libraryFile.OriginalFileName);
            }
        }

        #endregion

        #region Edge Cases and Integration Tests

        [TestMethod]
        public void Constructor_WithUnicodeFileName_HandlesCorrectly()
        {
            // Arrange
            string unicodeFileName = "测试文档-émojis🚀.pdf";
            string unicodePath = "lib_test/测试文档-émojis🚀.pdf";

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                originalFileName: unicodeFileName,
                relativePath: unicodePath);

            // Assert
            Assert.AreEqual(unicodeFileName, libraryFile.OriginalFileName);
            Assert.AreEqual(unicodePath, libraryFile.RelativePath);
        }

        [TestMethod]
        public void Constructor_WithVeryLongFileName_HandlesCorrectly()
        {
            // Arrange
            string longFileName = new string('a', 300) + ".pdf";
            string longPath = "lib_test/" + longFileName;

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                originalFileName: longFileName,
                relativePath: longPath);

            // Assert
            Assert.AreEqual(longFileName, libraryFile.OriginalFileName);
            Assert.AreEqual(longPath, libraryFile.RelativePath);
        }

        [TestMethod]
        public void Constructor_WithSpecialCharactersInPath_HandlesCorrectly()
        {
            // Arrange
            string specialFileName = "file with spaces & special-chars_123.pdf";
            string specialPath = "lib_test/folder with spaces/file with spaces & special-chars_123.pdf";

            // Act
            LibraryFile libraryFile = CreateTestLibraryFile(
                originalFileName: specialFileName,
                relativePath: specialPath);

            // Assert
            Assert.AreEqual(specialFileName, libraryFile.OriginalFileName);
            Assert.AreEqual(specialPath, libraryFile.RelativePath);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test LibraryFile with default values, allowing specific properties to be overridden.
        /// </summary>
        private LibraryFile CreateTestLibraryFile(
            Guid? id = null,
            Guid? libraryId = null,
            string originalFileName = "test-file.txt",
            string contentType = "text/plain",
            long sizeInBytes = 1024,
            string relativePath = "lib_test/test-file.txt",
            byte[]? hash = null,
            Guid? uploadedByUserId = null,
            DateTime? uploadedAt = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
        {
            DateTime now = DateTime.UtcNow;

            return new LibraryFile(
                id ?? Guid.NewGuid(),
                libraryId ?? Guid.NewGuid(),
                originalFileName,
                contentType,
                sizeInBytes,
                relativePath,
                hash ?? CreateTestHash(),
                uploadedByUserId ?? Guid.NewGuid(),
                uploadedAt ?? now.AddMinutes(-10),
                createdAt ?? now.AddMinutes(-8),
                updatedAt ?? now.AddMinutes(-5)
            );
        }

        /// <summary>
        /// Creates a test hash byte array.
        /// </summary>
        private static byte[] CreateTestHash()
        {
            return new byte[]
            {
                0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
                0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00
            };
        }

        /// <summary>
        /// Creates a proper SHA-256 hash (32 bytes).
        /// </summary>
        private static byte[] CreateTestSha256Hash()
        {
            byte[] hash = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                hash[i] = (byte)(i * 8 % 256);
            }
            return hash;
        }

        #endregion
    }
}

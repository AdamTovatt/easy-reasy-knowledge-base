using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Models.Dto;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Models.Dto
{
    /// <summary>
    /// Unit tests for the LibraryFileDto class.
    /// </summary>
    [TestClass]
    public class LibraryFileDtoTests : GeneralTestBase
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
            Guid fileId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            string originalFileName = "test.pdf";
            string contentType = "application/pdf";
            long sizeInBytes = 1024;
            DateTime uploadedAt = DateTime.UtcNow;
            string relativePath = "lib_123/test.pdf";
            string hashHex = "A1B2C3D4E5F6789";
            Guid uploadedByUserId = Guid.NewGuid();

            // Act
            LibraryFileDto dto = new LibraryFileDto(
                fileId, libraryId, originalFileName, contentType, 
                sizeInBytes, uploadedAt, relativePath, hashHex, uploadedByUserId);

            // Assert
            Assert.AreEqual(fileId, dto.FileId);
            Assert.AreEqual(libraryId, dto.LibraryId);
            Assert.AreEqual(originalFileName, dto.OriginalFileName);
            Assert.AreEqual(contentType, dto.ContentType);
            Assert.AreEqual(sizeInBytes, dto.SizeInBytes);
            Assert.AreEqual(uploadedAt, dto.UploadedAt);
            Assert.AreEqual(relativePath, dto.RelativePath);
            Assert.AreEqual(hashHex, dto.HashHex);
            Assert.AreEqual(uploadedByUserId, dto.UploadedByUserId);
        }

        [TestMethod]
        public void Constructor_WithNullOriginalFileName_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                CreateTestDto(originalFileName: null!));

            Assert.AreEqual("originalFileName", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_WithNullContentType_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                CreateTestDto(contentType: null!));

            Assert.AreEqual("contentType", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_WithNullRelativePath_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                CreateTestDto(relativePath: null!));

            Assert.AreEqual("relativePath", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_WithNullHashHex_ThrowsArgumentNullException()
        {
            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                CreateTestDto(hashHex: null!));

            Assert.AreEqual("hashHex", exception.ParamName);
        }

        #endregion

        #region FromFile Static Method Tests

        [TestMethod]
        public void FromFile_WithValidLibraryFile_CreatesCorrectDto()
        {
            // Arrange
            byte[] testHash = { 0xAB, 0xCD, 0xEF, 0x12, 0x34 };
            string expectedHashHex = Convert.ToHexString(testHash);

            LibraryFile libraryFile = new LibraryFile(
                id: Guid.NewGuid(),
                libraryId: Guid.NewGuid(),
                originalFileName: "document.docx",
                contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                sizeInBytes: 2048,
                relativePath: "lib_456/document.docx",
                hash: testHash,
                uploadedByUserId: Guid.NewGuid(),
                uploadedAt: DateTime.UtcNow.AddHours(-2),
                createdAt: DateTime.UtcNow.AddHours(-2),
                updatedAt: DateTime.UtcNow.AddHours(-1)
            );

            // Act
            LibraryFileDto dto = LibraryFileDto.FromFile(libraryFile);

            // Assert
            Assert.AreEqual(libraryFile.Id, dto.FileId);
            Assert.AreEqual(libraryFile.LibraryId, dto.LibraryId);
            Assert.AreEqual(libraryFile.OriginalFileName, dto.OriginalFileName);
            Assert.AreEqual(libraryFile.ContentType, dto.ContentType);
            Assert.AreEqual(libraryFile.SizeInBytes, dto.SizeInBytes);
            Assert.AreEqual(libraryFile.UploadedAt, dto.UploadedAt);
            Assert.AreEqual(libraryFile.RelativePath, dto.RelativePath);
            Assert.AreEqual(expectedHashHex, dto.HashHex);
            Assert.AreEqual(libraryFile.UploadedByUserId, dto.UploadedByUserId);
        }

        [TestMethod]
        public void FromFile_HashHex_IsUppercase()
        {
            // Arrange - Hash with lowercase hex values when converted
            byte[] testHash = { 0xab, 0xcd, 0xef };
            LibraryFile libraryFile = CreateTestLibraryFile(hash: testHash);

            // Act
            LibraryFileDto dto = LibraryFileDto.FromFile(libraryFile);

            // Assert
            Assert.AreEqual("ABCDEF", dto.HashHex);
            Assert.IsTrue(dto.HashHex.All(c => !char.IsLower(c) || !char.IsLetter(c)));
        }

        [TestMethod]
        public void FromFile_WithEmptyHash_CreatesEmptyHashHex()
        {
            // Arrange
            byte[] emptyHash = Array.Empty<byte>();
            LibraryFile libraryFile = CreateTestLibraryFile(hash: emptyHash);

            // Act
            LibraryFileDto dto = LibraryFileDto.FromFile(libraryFile);

            // Assert
            Assert.AreEqual("", dto.HashHex);
        }

        #endregion

        #region FormattedSize Property Tests

        [TestMethod]
        public void FormattedSize_WithZeroBytes_ReturnsCorrectFormat()
        {
            // Arrange
            LibraryFileDto dto = CreateTestDto(sizeInBytes: 0);

            // Act & Assert
            Assert.AreEqual("0 B", dto.FormattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithKilobytes_ReturnsCorrectFormat()
        {
            // Arrange
            LibraryFileDto dto = CreateTestDto(sizeInBytes: 2048);

            // Act & Assert
            Assert.AreEqual("2 KB", dto.FormattedSize);
        }

        [TestMethod]
        public void FormattedSize_WithMegabytes_ReturnsCorrectFormat()
        {
            // Arrange
            LibraryFileDto dto = CreateTestDto(sizeInBytes: 5242880); // 5 MB

            // Act & Assert
            Assert.AreEqual("5 MB", dto.FormattedSize);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void FromFile_ThenAccess_WorksCorrectly()
        {
            // Arrange
            LibraryFile libraryFile = CreateTestLibraryFile();

            // Act
            LibraryFileDto dto = LibraryFileDto.FromFile(libraryFile);

            // Assert - Verify all essential properties are accessible
            Assert.IsNotNull(dto.OriginalFileName);
            Assert.IsNotNull(dto.ContentType);
            Assert.IsNotNull(dto.RelativePath);
            Assert.IsNotNull(dto.HashHex);
            Assert.IsNotNull(dto.FormattedSize);
            Assert.IsTrue(dto.SizeInBytes >= 0);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test LibraryFileDto with default values, allowing specific properties to be overridden.
        /// </summary>
        private LibraryFileDto CreateTestDto(
            Guid? fileId = null,
            Guid? libraryId = null,
            string originalFileName = "test.txt",
            string contentType = "text/plain",
            long sizeInBytes = 1024,
            DateTime? uploadedAt = null,
            string relativePath = "lib_test/test.txt",
            string hashHex = "ABCDEF123456",
            Guid? uploadedByUserId = null)
        {
            return new LibraryFileDto(
                fileId ?? Guid.NewGuid(),
                libraryId ?? Guid.NewGuid(),
                originalFileName,
                contentType,
                sizeInBytes,
                uploadedAt ?? DateTime.UtcNow.AddMinutes(-30),
                relativePath,
                hashHex,
                uploadedByUserId ?? Guid.NewGuid()
            );
        }

        /// <summary>
        /// Creates a test LibraryFile for conversion testing.
        /// </summary>
        private LibraryFile CreateTestLibraryFile(byte[]? hash = null)
        {
            return new LibraryFile(
                id: Guid.NewGuid(),
                libraryId: Guid.NewGuid(),
                originalFileName: "test-file.txt",
                contentType: "text/plain",
                sizeInBytes: 1024,
                relativePath: "lib_test/test-file.txt",
                hash: hash ?? new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB },
                uploadedByUserId: Guid.NewGuid(),
                uploadedAt: DateTime.UtcNow.AddMinutes(-10),
                createdAt: DateTime.UtcNow.AddMinutes(-8),
                updatedAt: DateTime.UtcNow.AddMinutes(-5)
            );
        }

        #endregion
    }
}

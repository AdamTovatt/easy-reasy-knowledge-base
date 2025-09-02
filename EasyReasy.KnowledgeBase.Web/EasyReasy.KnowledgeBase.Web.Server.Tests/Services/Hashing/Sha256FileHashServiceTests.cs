using EasyReasy.KnowledgeBase.Web.Server.Services.Hashing;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Services.Hashing
{
    /// <summary>
    /// Unit tests for the Sha256FileHashService.
    /// </summary>
    [TestClass]
    public class Sha256FileHashServiceTests : GeneralTestBase
    {
        private Sha256FileHashService _hashService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _hashService = new Sha256FileHashService();
            LoadTestEnvironmentVariables();
        }

        #region ComputeHashAsync with Stream Tests

        [TestMethod]
        public async Task ComputeHashAsync_WithValidStream_ReturnsCorrectHash()
        {
            // Arrange
            string testContent = "Hello, World!";
            byte[] testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
            using MemoryStream stream = new MemoryStream(testBytes);

            // Act
            byte[] hash = await _hashService.ComputeHashAsync(stream);

            // Assert
            Assert.IsNotNull(hash);
            Assert.AreEqual(32, hash.Length); // SHA-256 produces 32 bytes

            // Verify it's a consistent hash by computing it again
            stream.Position = 0;
            byte[] hash2 = await _hashService.ComputeHashAsync(stream);
            CollectionAssert.AreEqual(hash, hash2);
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithEmptyStream_ReturnsValidHash()
        {
            // Arrange
            using MemoryStream emptyStream = new MemoryStream();

            // Act
            byte[] hash = await _hashService.ComputeHashAsync(emptyStream);

            // Assert
            Assert.IsNotNull(hash);
            Assert.AreEqual(32, hash.Length);

            // Empty content should produce a specific, known hash
            string hexHash = Convert.ToHexString(hash);
            Assert.AreEqual("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", hexHash);
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            Stream? nullStream = null;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _hashService.ComputeHashAsync(nullStream!));
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithNonSeekableStream_ReturnsValidHash()
        {
            // Arrange
            string testContent = "Test content for non-seekable stream";
            byte[] testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
            using MemoryStream baseStream = new MemoryStream(testBytes);
            using NonSeekableStream nonSeekableStream = new NonSeekableStream(baseStream);

            // Act
            byte[] hash = await _hashService.ComputeHashAsync(nonSeekableStream);

            // Assert
            Assert.IsNotNull(hash);
            Assert.AreEqual(32, hash.Length);
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithLargeStream_ReturnsValidHash()
        {
            // Arrange
            byte[] largeContent = new byte[1024 * 1024]; // 1MB
            Random.Shared.NextBytes(largeContent);
            using MemoryStream largeStream = new MemoryStream(largeContent);

            // Act
            byte[] hash = await _hashService.ComputeHashAsync(largeStream);

            // Assert
            Assert.IsNotNull(hash);
            Assert.AreEqual(32, hash.Length);
        }

        #endregion

        #region ComputeHashAsync with FilePath Tests

        [TestMethod]
        public async Task ComputeHashAsync_WithValidFilePath_ReturnsCorrectHash()
        {
            // Arrange
            string tempFilePath = Path.GetTempFileName();
            string testContent = "Test file content";
            await File.WriteAllTextAsync(tempFilePath, testContent);

            try
            {
                // Act
                byte[] hash = await _hashService.ComputeHashAsync(tempFilePath);

                // Assert
                Assert.IsNotNull(hash);
                Assert.AreEqual(32, hash.Length);

                // Verify consistency by computing hash again
                byte[] hash2 = await _hashService.ComputeHashAsync(tempFilePath);
                CollectionAssert.AreEqual(hash, hash2);
            }
            finally
            {
                // Cleanup
                File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithEmptyFile_ReturnsValidHash()
        {
            // Arrange
            string tempFilePath = Path.GetTempFileName();

            try
            {
                // File is already empty after GetTempFileName()

                // Act
                byte[] hash = await _hashService.ComputeHashAsync(tempFilePath);

                // Assert
                Assert.IsNotNull(hash);
                Assert.AreEqual(32, hash.Length);

                // Should be same as empty stream hash
                string hexHash = Convert.ToHexString(hash);
                Assert.AreEqual("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", hexHash);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithNullFilePath_ThrowsArgumentException()
        {
            // Arrange
            string? nullPath = null;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _hashService.ComputeHashAsync(nullPath!));
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithEmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            string emptyPath = "";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _hashService.ComputeHashAsync(emptyPath));
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithWhitespaceFilePath_ThrowsArgumentException()
        {
            // Arrange
            string whitespacePath = "   ";

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _hashService.ComputeHashAsync(whitespacePath));
        }

        [TestMethod]
        public async Task ComputeHashAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(
                () => _hashService.ComputeHashAsync(nonExistentPath));
        }

        #endregion

        #region ToHexString Tests

        [TestMethod]
        public void ToHexString_WithValidHash_ReturnsCorrectHexString()
        {
            // Arrange
            byte[] testHash = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

            // Act
            string hexString = _hashService.ToHexString(testHash);

            // Assert
            Assert.AreEqual("0123456789ABCDEF", hexString);
        }

        [TestMethod]
        public void ToHexString_WithEmptyHash_ReturnsEmptyString()
        {
            // Arrange
            byte[] emptyHash = Array.Empty<byte>();

            // Act
            string hexString = _hashService.ToHexString(emptyHash);

            // Assert
            Assert.AreEqual("", hexString);
        }

        [TestMethod]
        public void ToHexString_WithNullHash_ThrowsArgumentNullException()
        {
            // Arrange
            byte[]? nullHash = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(
                () => _hashService.ToHexString(nullHash!));
        }

        [TestMethod]
        public void ToHexString_WithTypicalSha256Hash_ReturnsUppercaseHex()
        {
            // Arrange
            byte[] sha256Hash = new byte[32]; // Typical SHA-256 length
            for (int i = 0; i < 32; i++)
            {
                sha256Hash[i] = (byte)(i % 256);
            }

            // Act
            string hexString = _hashService.ToHexString(sha256Hash);

            // Assert
            Assert.AreEqual(64, hexString.Length); // 32 bytes = 64 hex characters
            Assert.IsTrue(hexString.All(c => char.IsUpper(c) || char.IsDigit(c)));
        }

        #endregion

        #region FromHexString Tests

        [TestMethod]
        public void FromHexString_WithValidHex_ReturnsCorrectBytes()
        {
            // Arrange
            string hexString = "0123456789ABCDEF";
            byte[] expectedBytes = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

            // Act
            byte[] actualBytes = _hashService.FromHexString(hexString);

            // Assert
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [TestMethod]
        public void FromHexString_WithLowercaseHex_ReturnsCorrectBytes()
        {
            // Arrange
            string hexString = "0123456789abcdef";
            byte[] expectedBytes = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

            // Act
            byte[] actualBytes = _hashService.FromHexString(hexString);

            // Assert
            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [TestMethod]
        public void FromHexString_WithEmptyString_ReturnsEmptyByteArray()
        {
            // Arrange
            string emptyHex = "";

            // Act
            byte[] result = _hashService.FromHexString(emptyHex);

            // Assert
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void FromHexString_WithNullString_ThrowsArgumentException()
        {
            // Arrange
            string? nullHex = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => _hashService.FromHexString(nullHex!));
        }

        [TestMethod]
        public void FromHexString_WithWhitespaceString_ThrowsArgumentException()
        {
            // Arrange
            string whitespaceHex = "   ";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => _hashService.FromHexString(whitespaceHex));
        }

        [TestMethod]
        public void FromHexString_WithOddLengthHex_ThrowsArgumentException()
        {
            // Arrange
            string oddLengthHex = "ABC"; // 3 characters (odd)

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => _hashService.FromHexString(oddLengthHex));
        }

        [TestMethod]
        public void FromHexString_WithInvalidHexCharacters_ThrowsFormatException()
        {
            // Arrange
            string invalidHex = "0123XYZ9"; // Contains X, Y, Z which are not valid hex

            // Act & Assert
            Assert.ThrowsException<FormatException>(
                () => _hashService.FromHexString(invalidHex));
        }

        #endregion

        #region Properties Tests

        [TestMethod]
        public void AlgorithmName_ReturnsCorrectValue()
        {
            // Act
            string algorithmName = _hashService.AlgorithmName;

            // Assert
            Assert.AreEqual("SHA-256", algorithmName);
        }

        [TestMethod]
        public void HashLength_ReturnsCorrectValue()
        {
            // Act
            int hashLength = _hashService.HashLength;

            // Assert
            Assert.AreEqual(32, hashLength); // SHA-256 produces 32 bytes
        }

        #endregion

        #region Round-trip Tests

        [TestMethod]
        public async Task RoundTrip_HashToHexAndBack_ProducesSameResult()
        {
            // Arrange
            string testContent = "Round-trip test content";
            byte[] testBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
            using MemoryStream stream = new MemoryStream(testBytes);

            // Act
            byte[] originalHash = await _hashService.ComputeHashAsync(stream);
            string hexString = _hashService.ToHexString(originalHash);
            byte[] roundTripHash = _hashService.FromHexString(hexString);

            // Assert
            CollectionAssert.AreEqual(originalHash, roundTripHash);
        }

        [TestMethod]
        public async Task RoundTrip_MultipleFiles_ProducesConsistentResults()
        {
            // Arrange
            List<string> testContents = new List<string>
            {
                "File 1 content",
                "Different content for file 2",
                "",
                "Special characters: éñ中文🚀"
            };

            List<byte[]> hashes = new List<byte[]>();
            List<string> hexStrings = new List<string>();

            // Act - First pass: compute hashes and convert to hex
            foreach (string content in testContents)
            {
                byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                using MemoryStream stream = new MemoryStream(contentBytes);

                byte[] hash = await _hashService.ComputeHashAsync(stream);
                string hexString = _hashService.ToHexString(hash);

                hashes.Add(hash);
                hexStrings.Add(hexString);
            }

            // Act - Second pass: convert hex back to bytes and verify
            for (int i = 0; i < testContents.Count; i++)
            {
                byte[] reconstructedHash = _hashService.FromHexString(hexStrings[i]);

                // Assert
                CollectionAssert.AreEqual(hashes[i], reconstructedHash,
                    $"Round-trip failed for content at index {i}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class to create a non-seekable stream for testing.
    /// </summary>
    public class NonSeekableStream : Stream
    {
        private readonly Stream _innerStream;

        public NonSeekableStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false; // Force non-seekable
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

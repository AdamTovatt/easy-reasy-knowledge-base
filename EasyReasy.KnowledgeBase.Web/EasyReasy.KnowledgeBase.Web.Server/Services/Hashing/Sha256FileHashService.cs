using System.Security.Cryptography;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Hashing
{
    /// <summary>
    /// Implementation of file hash service using SHA-256 algorithm.
    /// </summary>
    public class Sha256FileHashService : IFileHashService
    {
        /// <inheritdoc/>
        public string AlgorithmName => "SHA-256";

        /// <inheritdoc/>
        public int HashLength => 32; // SHA-256 produces 32 bytes (256 bits)

        /// <inheritdoc/>
        public async Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Reset stream position to beginning to ensure we hash the entire content
            if (stream.CanSeek)
                stream.Position = 0;

            using (SHA256 sha256 = SHA256.Create())
            {
                return await sha256.ComputeHashAsync(stream, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                return await ComputeHashAsync(fileStream, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public string ToHexString(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            return Convert.ToHexString(hash);
        }

        /// <inheritdoc/>
        public byte[] FromHexString(string hexString)
        {
            if (hexString == null)
                throw new ArgumentException("Hex string cannot be null.", nameof(hexString));

            if (hexString.Length == 0)
                return Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(hexString))
                throw new ArgumentException("Hex string cannot be null, empty, or whitespace.", nameof(hexString));

            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even number of characters.", nameof(hexString));

            return Convert.FromHexString(hexString);
        }
    }
}

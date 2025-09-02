namespace EasyReasy.KnowledgeBase.Web.Server.Services.Hashing
{
    /// <summary>
    /// Service for computing file hashes consistently across the application.
    /// </summary>
    public interface IFileHashService
    {
        /// <summary>
        /// Computes the hash of data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read data from. The stream position will be reset to the beginning.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The computed hash as a byte array.</returns>
        Task<byte[]> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Computes the hash of a file at the specified path.
        /// </summary>
        /// <param name="filePath">The path to the file to hash.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The computed hash as a byte array.</returns>
        Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts a hash byte array to a hexadecimal string representation.
        /// </summary>
        /// <param name="hash">The hash bytes to convert.</param>
        /// <returns>The hash as a hexadecimal string (uppercase).</returns>
        string ToHexString(byte[] hash);

        /// <summary>
        /// Converts a hexadecimal string back to a hash byte array.
        /// </summary>
        /// <param name="hexString">The hexadecimal string to convert.</param>
        /// <returns>The hash as a byte array.</returns>
        byte[] FromHexString(string hexString);

        /// <summary>
        /// Gets the name of the hash algorithm being used (e.g., "SHA-256").
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// Gets the expected length in bytes of hashes produced by this service.
        /// </summary>
        int HashLength { get; }
    }
}

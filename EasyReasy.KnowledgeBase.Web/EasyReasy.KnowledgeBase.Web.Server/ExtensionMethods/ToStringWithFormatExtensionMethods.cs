using System.Globalization;

namespace EasyReasy.KnowledgeBase.Web.Server.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods for producing culture-invariant formatted strings.
    /// </summary>
    public static class ToStringWithFormatExtensionMethods
    {
        /// <summary>
        /// Formats a file size, expressed in bytes, into a human-readable string using base-1024 units.
        /// </summary>
        /// <param name="sizeInBytes">The file size in bytes.</param>
        /// <returns>
        /// A culture-invariant string such as "0 B", "1.5 KB", or "12.34 MB".
        /// </returns>
        /// <remarks>
        /// Uses unit suffixes B, KB, MB, GB, and TB, and displays up to two fractional digits
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// long value = 1536;
        /// string text = value.FormatFileSize(); // "1.5 KB"
        /// </code>
        /// </example>
        public static string ToFileSizeString(this long sizeInBytes)
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

            return $"{size.ToString("0.##", CultureInfo.InvariantCulture)} {suffixes[suffixIndex]}";
        }
    }
}

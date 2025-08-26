﻿namespace EasyReasy.KnowledgeBase.Chunking
{
    /// <summary>
    /// A knowledge chunk reader that processes markdown content using token-based chunking.
    /// </summary>
    public class SegmentBasedChunkReader : IKnowledgeChunkReader
    {
        private readonly ITextSegmentReader _textSegmentReader;
        private readonly ChunkingConfiguration _configuration;
        private readonly ITokenizer _tokenizer;
        private string? _bufferedChunk;

        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentBasedChunkReader"/> class.
        /// </summary>
        /// <param name="textSegmentReader">The reader to read the content from.</param>
        /// <param name="configuration">The configuration for chunking operations.</param>
        public SegmentBasedChunkReader(TextSegmentReader textSegmentReader, ChunkingConfiguration configuration)
        {
            _configuration = configuration;
            _tokenizer = configuration.Tokenizer;

            _textSegmentReader = textSegmentReader ??
                throw new ArgumentNullException(nameof(textSegmentReader), "Text segment reader cannot be null.");

            _bufferedChunk = null;
        }

        /// <summary>
        /// Reads the next chunk of content from the markdown stream.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The next chunk of content as a string, or null if no more content is available.</returns>
        public async Task<string?> ReadNextChunkContentAsync(CancellationToken cancellationToken = default)
        {
            // If we have a buffered chunk, start with it
            string currentChunk = _bufferedChunk ?? string.Empty;
            _bufferedChunk = null;

            // If we don't have any buffered content, read the first segment
            if (string.IsNullOrEmpty(currentChunk))
            {
                string? firstSegment = await _textSegmentReader.ReadNextTextSegmentAsync(cancellationToken);
                if (firstSegment == null)
                    return null;

                currentChunk = firstSegment;
            }

            // Check if the current chunk is already at or over the token limit
            int currentTokens = _tokenizer.CountTokens(currentChunk);
            if (currentTokens >= _configuration.MaxTokensPerChunk)
            {
                // Current chunk is already at or over the limit, return it as-is
                return currentChunk;
            }

            // Keep reading segments and adding them to the chunk until we reach the token limit
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? nextSegment = await _textSegmentReader.ReadNextTextSegmentAsync(cancellationToken);
                if (nextSegment == null)
                {
                    // No more content available, return what we have
                    return string.IsNullOrEmpty(currentChunk) ? null : currentChunk;
                }

                // Check if the next segment starts with a stop signal
                if (StartsWithStopSignal(nextSegment))
                {
                    // Next segment starts with a stop signal, buffer it for the next chunk
                    _bufferedChunk = nextSegment;
                    return currentChunk;
                }

                // Check if adding this segment would exceed the token limit
                string potentialChunk = currentChunk + nextSegment;
                int potentialTokens = _tokenizer.CountTokens(potentialChunk);

                if (potentialTokens <= _configuration.MaxTokensPerChunk)
                {
                    // Adding this segment keeps us within the limit, add it to the current chunk
                    currentChunk = potentialChunk;
                }
                else
                {
                    // Adding this segment would exceed the limit
                    // Buffer it for the next chunk and return the current chunk
                    _bufferedChunk = nextSegment;
                    return currentChunk;
                }
            }
        }

        /// <summary>
        /// Checks if the given segment starts with any of the configured chunk stop signals.
        /// </summary>
        /// <param name="segment">The segment to check.</param>
        /// <returns>True if the segment starts with a stop signal, false otherwise.</returns>
        private bool StartsWithStopSignal(string segment)
        {
            if (string.IsNullOrEmpty(segment) || _configuration.ChunkStopSignals.Length == 0)
                return false;

            foreach (string stopSignal in _configuration.ChunkStopSignals)
            {
                if (!string.IsNullOrEmpty(stopSignal) && segment.StartsWith(stopSignal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

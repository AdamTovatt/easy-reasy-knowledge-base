﻿using EasyReasy.KnowledgeBase.Storage;

namespace EasyReasy.KnowledgeBase.Searching
{
    /// <summary>
    /// Defines the contract for a searchable knowledge store that provides vector-based search capabilities.
    /// </summary>
    public interface ISearchableKnowledgeStore : IKnowledgeStore
    {
        /// <summary>
        /// Gets the vector store for searching all chunks across all sections.
        /// </summary>
        /// <returns>A vector store for chunk-level searches.</returns>
        IKnowledgeVectorStore GetChunksVectorStore();
    }
}

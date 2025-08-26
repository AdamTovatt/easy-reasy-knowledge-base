# EasyReasy.KnowledgeBase

A comprehensive .NET library ecosystem for knowledge base management with vector search capabilities, intelligent document chunking, and AI-powered content processing.

## Overview

EasyReasy.KnowledgeBase is a modular system designed for building intelligent knowledge management applications. It provides a complete solution for processing, storing, and searching through large documents using semantic similarity and AI-powered analysis.

## Core Philosophy

**No Forced Dependencies**: The core library provides all interfaces and logic, but implementations are in separate packages. You can choose which implementations to use or create your own.

## Available Packages

### Core Library
- **[EasyReasy.KnowledgeBase](EasyReasy.KnowledgeBase/README.md)** - Core library with interfaces, models, and intelligent chunking algorithms

### Storage Implementations
- **[EasyReasy.KnowledgeBase.Storage.Sqlite](EasyReasy.KnowledgeBase.Storage.Sqlite/README.md)** - SQLite-based persistent storage with automatic schema management
- **[EasyReasy.KnowledgeBase.Storage.IntegratedVectorStore](EasyReasy.KnowledgeBase.Storage.IntegratedVectorStore/README.md)** - Vector storage integration for similarity search

### Tokenization
- **[EasyReasy.KnowledgeBase.BertTokenization](EasyReasy.KnowledgeBase.BertTokenization/README.md)** - BERT-based tokenization using FastBertTokenizer

### AI Generation & Embeddings
- **[EasyReasy.KnowledgeBase.OllamaGeneration](EasyReasy.KnowledgeBase.OllamaGeneration/README.md)** - Ollama integration for embeddings and AI generation services

### Test Projects
- **[EasyReasy.KnowledgeBase.Tests](EasyReasy.KnowledgeBase.Tests/)** - Core library tests
- **[EasyReasy.KnowledgeBase.BertTokenization.Tests](EasyReasy.KnowledgeBase.BertTokenization.Tests/)** - BERT tokenization tests
- **[EasyReasy.KnowledgeBase.OllamaGeneration.Tests](EasyReasy.KnowledgeBase.OllamaGeneration.Tests/)** - Ollama integration tests
- **[EasyReasy.KnowledgeBase.Storage.Sqlite.Tests](EasyReasy.KnowledgeBase.Storage.Sqlite.Tests/)** - SQLite storage tests

## Key Features

- **🧠 Smart AI Semantic Sectioning**: Uses embedding similarity and statistical analysis to intelligently group related chunks
- **📊 Adaptive Thresholds**: Automatically determines section boundaries using standard deviation analysis
- **💾 Memory Efficient**: Three-tier streaming architecture for processing large documents
- **🔍 Intelligent Search**: Vector-based similarity search with comprehensive confidence ratings
- **🤖 AI Generation**: Built-in services for summarization, question generation, and contextualization
- **🛠️ Modular Design**: Choose only the components you need with no forced dependencies

## Quick Start

```csharp
// Set up services
BertTokenizer tokenizer = await BertTokenizer.CreateAsync();
EasyReasyOllamaEmbeddingService embeddingService = await EasyReasyOllamaEmbeddingService.CreateAsync(
    baseUrl: "https://your-ollama-server.com",
    modelName: "nomic-embed-text");

// Create storage
SqliteKnowledgeStore knowledgeStore = await SqliteKnowledgeStore.CreateAsync("knowledge.db");
CosineVectorStore cosineVectorStore = new CosineVectorStore(embeddingService.Dimensions);
IKnowledgeVectorStore vectorStore = new EasyReasyVectorStore(cosineVectorStore);

// Create searchable knowledge base
ISearchableKnowledgeBase knowledgeBase = new SearchableKnowledgeBase(
    new SearchableKnowledgeStore(knowledgeStore, vectorStore), 
    embeddingService, 
    tokenizer);

// Index documents
IIndexer indexer = knowledgeBase.CreateIndexer();
foreach (IFileSource fileSource in await fileSourceProvider.GetAllFilesAsync())
{
    await indexer.ConsumeAsync(fileSource);
}

// Search for relevant content
IKnowledgeBaseSearchResult result = await knowledgeBase.SearchAsync("your query", maxSearchResultsCount: 10);
string contextString = result.GetAsContextString();
```

## Installation

Install the packages you need:

```bash
# Core library
dotnet add package EasyReasy.KnowledgeBase

# Storage implementations
dotnet add package EasyReasy.KnowledgeBase.Storage.Sqlite
dotnet add package EasyReasy.KnowledgeBase.Storage.IntegratedVectorStore

# Tokenization
dotnet add package EasyReasy.KnowledgeBase.BertTokenization

# AI Generation
dotnet add package EasyReasy.KnowledgeBase.OllamaGeneration
```

## Documentation

Each package has its own detailed documentation. Start with the [core library](EasyReasy.KnowledgeBase/README.md) for a complete overview of the system architecture and capabilities.

## License

MIT License - see individual package documentation for details.

## Contributing

This is part of the EasyReasy ecosystem. For contribution guidelines and more information, visit the main EasyReasy repository.

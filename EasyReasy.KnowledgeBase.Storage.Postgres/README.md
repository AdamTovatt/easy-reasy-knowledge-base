# EasyReasy.KnowledgeBase.Storage.Postgres

[← Back to EasyReasy.KnowledgeBase](../README.md)

[![NuGet](https://img.shields.io/badge/nuget-EasyReasy.KnowledgeBase.Storage.Postgres-blue.svg)](https://www.nuget.org/packages/EasyReasy.KnowledgeBase.Storage.Postgres)

PostgreSQL storage provider for EasyReasy KnowledgeBase with persistent data storage using connection factory pattern.

## Overview

This package provides a complete PostgreSQL implementation of the EasyReasy KnowledgeBase storage interfaces. It uses a connection factory pattern to manage database connections efficiently and supports all the core storage operations for knowledge files, sections, and chunks.

## Features

- **Connection Factory Pattern**: Efficient connection management with `IDbConnectionFactory`
- **PostgreSQL Native Types**: Uses PostgreSQL-specific data types (UUID, BYTEA, TIMESTAMP WITH TIME ZONE)
- **Automatic Schema Creation**: Tables and indexes are created automatically on first use
- **Performance Optimized**: Includes database indexes for common query patterns
- **Thread Safe**: Each operation gets its own database connection
- **Nullable Reference Support**: Full support for nullable reference types

## Installation

```bash
dotnet add package EasyReasy.KnowledgeBase.Storage.Postgres
```

## Quick Start

### Basic Usage

```csharp
using EasyReasy.KnowledgeBase.Storage.Postgres;

// Create a knowledge store with connection string
string connectionString = "Host=localhost;Database=knowledgebase;Username=user;Password=pass";
PostgresKnowledgeStore knowledgeStore = PostgresKnowledgeStore.Create(connectionString);

// Use the storage components
await knowledgeStore.Files.AddAsync(file);
await knowledgeStore.Sections.AddAsync(section);
await knowledgeStore.Chunks.AddAsync(chunk);
```

### Using Connection Factory

```csharp
// Create a custom connection factory
IDbConnectionFactory connectionFactory = new PostgresConnectionFactory(connectionString);

// Create the knowledge store with the factory
PostgresKnowledgeStore knowledgeStore = PostgresKnowledgeStore.Create(connectionFactory);
```

### Advanced Usage with Dependency Injection

```csharp
// Register services in your DI container
services.AddSingleton<IDbConnectionFactory>(provider => 
    new PostgresConnectionFactory(connectionString));
services.AddSingleton<IKnowledgeStore>(provider => 
    PostgresKnowledgeStore.Create(provider.GetRequiredService<IDbConnectionFactory>()));
```

## Database Schema

The implementation creates the following tables with data integrity constraints:

### knowledge_files
- `id` (UUID PRIMARY KEY) - Unique file identifier
- `name` (TEXT) - File name
- `hash` (BYTEA) - Content hash for change detection
- `processed_at` (TIMESTAMP WITH TIME ZONE) - Processing timestamp
- `status` (INTEGER) - Processing status enum value

### knowledge_sections
- `id` (UUID PRIMARY KEY) - Unique section identifier
- `file_id` (UUID) - Reference to parent file
- `section_index` (INTEGER) - Zero-based index within file
- `summary` (TEXT) - Optional section summary
- `additional_context` (TEXT) - Optional additional context
- **UNIQUE CONSTRAINT**: `(file_id, section_index)` - Ensures unique section indexes per file

### knowledge_chunks
- `id` (UUID PRIMARY KEY) - Unique chunk identifier
- `section_id` (UUID) - Reference to parent section
- `chunk_index` (INTEGER) - Zero-based index within section
- `content` (TEXT) - Chunk content
- `embedding` (BYTEA) - Optional vector embedding
- `file_id` (UUID) - Reference to parent file
- **UNIQUE CONSTRAINT**: `(section_id, chunk_index)` - Ensures unique chunk indexes per section

## Example Database Schema

The complete SQL schema used by the PostgreSQL implementation:

```sql
-- Create knowledge_files table
CREATE TABLE IF NOT EXISTS knowledge_files (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    hash BYTEA NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    status INTEGER NOT NULL
);

-- Create knowledge_sections table
CREATE TABLE IF NOT EXISTS knowledge_sections (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    section_index INTEGER NOT NULL,
    summary TEXT,
    additional_context TEXT,
    UNIQUE (file_id, section_index)
);

-- Create knowledge_chunks table
CREATE TABLE IF NOT EXISTS knowledge_chunks (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding BYTEA,
    file_id UUID NOT NULL,
    UNIQUE (section_id, chunk_index)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_chunks_section_id ON knowledge_chunks (section_id);
CREATE INDEX IF NOT EXISTS idx_chunks_file_id ON knowledge_chunks (file_id);
CREATE INDEX IF NOT EXISTS idx_chunks_section_index ON knowledge_chunks (section_id, chunk_index);
CREATE INDEX IF NOT EXISTS idx_sections_file_id ON knowledge_sections (file_id);
CREATE INDEX IF NOT EXISTS idx_sections_file_index ON knowledge_sections (file_id, section_index);
```

## Data Integrity & Performance

The implementation includes several features to ensure data integrity and optimal performance:

### Unique Constraints
- **Section uniqueness**: Each file can only have one section per index
- **Chunk uniqueness**: Each section can only have one chunk per index
- **Prevents duplicate data**: Ensures logical consistency in the knowledge base structure

### Performance Indexes
- `idx_chunks_section_id` - For chunk lookups by section
- `idx_chunks_file_id` - For chunk lookups by file  
- `idx_chunks_section_index` - For ordered chunk retrieval within sections
- `idx_sections_file_id` - For section lookups by file
- `idx_sections_file_index` - For ordered section retrieval within files

All indexes and constraints are created automatically during initialization.

## Connection Management

The connection factory pattern provides several benefits:

1. **Short-lived Connections**: Each operation gets a fresh connection
2. **Automatic Disposal**: Connections are properly disposed using `using` statements
3. **Thread Safety**: Multiple operations can run concurrently
4. **Connection Pooling**: Npgsql handles connection pooling automatically
5. **Testability**: Easy to mock for unit tests

## Error Handling

The implementation includes comprehensive error handling:

- **Null Checks**: All public methods validate input parameters
- **Database Errors**: PostgreSQL-specific exceptions are propagated
- **Schema Initialization**: Automatic table creation with error handling
- **Transaction Safety**: Each operation is atomic

## Migration from SQLite

If you're migrating from the SQLite implementation, note these key differences:

1. **No IExplicitPersistence**: PostgreSQL doesn't require explicit load/save operations
2. **Connection Factory**: Uses `IDbConnectionFactory` instead of connection strings directly
3. **PostgreSQL Types**: Uses UUID instead of TEXT for IDs, BYTEA for binary data
4. **Timestamp Handling**: Uses `TIMESTAMP WITH TIME ZONE` for proper timezone support

## Testing

The implementation is designed to be easily testable:

```csharp
// Mock the connection factory for unit tests
var mockFactory = new Mock<IDbConnectionFactory>();
var knowledgeStore = PostgresKnowledgeStore.Create(mockFactory.Object);
```

## Requirements

- .NET 8.0 or later
- PostgreSQL 12.0 or later
- Npgsql 8.0.2 or later

## License

MIT License - see the main project license for details.

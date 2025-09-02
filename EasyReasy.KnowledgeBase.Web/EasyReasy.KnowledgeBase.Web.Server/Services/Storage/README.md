# File Storage Service - Potential Improvements

This document outlines areas where the current file storage implementation could be enhanced or features that might be missing depending on requirements.

## Security Concerns 🔒

- **No file type validation** - Users can upload any file type (`.exe`, scripts, etc.)
- **No virus scanning** - Uploaded files aren't scanned for malware

## Performance/Scalability Issues ⚡

- **Memory cache for sessions** - Won't work if you scale to multiple server instances
- **No file deduplication** - Same files uploaded multiple times take up space
- **No pagination** - `ListFiles` could be slow with thousands of files
- **No file compression** - Large files stored as-is

## Robustness/Reliability 🛡️

- **No orphaned file cleanup** - If database writes fail after file storage, files become orphaned
- **Session cleanup** - No background job to clean up expired/abandoned upload sessions
- **No file integrity checks** - No checksums to verify file wasn't corrupted
- **No backup/disaster recovery** - Files only exist in one location

## Missing Features 📋

- **File versioning** - Can't track file history or updates
- **Knowledge base permissions** - Who can read/write to which knowledge bases?
- **File metadata search** - Can't search by filename, content type, etc.
- **Bulk operations** - No bulk delete, download multiple files as zip
- **File thumbnails/previews** - For images, PDFs, etc.

## Operational Issues 🔧

- **No metrics/monitoring** - Storage usage, upload success rates, etc.
- **No admin tools** - Can't see storage usage, clean up files, etc.
- **No rate limiting** - Users could spam uploads

## Priority Recommendations

### Recently Implemented ✅
- **File size limits** - Configurable maximum file size via `MAX_FILE_SIZE_BYTES` environment variable (defaults to 500MB)
- **Authorization system** - Complete knowledge base permission system with read/write/admin levels
- **Database schema** - PostgreSQL enum-based permission types with proper foreign key relationships
- **Service layer security** - All FileStorageService methods now require user authentication and validate permissions
- **File hashing** - SHA-256 hash computation and storage for all uploaded files via `IFileHashService`
- **Hash service abstraction** - Pluggable hashing implementation with `Sha256FileHashService` for consistent hash computation across services

### Critical (Should Address Soon)
1. **Session storage** - Use Redis instead of memory cache for scalability  
2. **File type validation** - Basic security against malicious uploads
3. **Orphaned file cleanup** - Prevent storage bloat

### Important (Future Enhancements)
- Background cleanup jobs
- Pagination for file listings

### Nice to Have
- File deduplication
- Compression
- Thumbnails/previews
- Bulk operations
- Metrics and monitoring

## Testing Requirements 🧪

The following test classes and methods should be implemented to ensure proper functionality of the file storage and hashing systems:

### Sha256FileHashServiceTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Hashing/Sha256FileHashServiceTests.cs`
- `ComputeHashAsync_WithValidStream_ReturnsCorrectHash`
- `ComputeHashAsync_WithNullStream_ThrowsArgumentNullException`
- `ComputeHashAsync_WithValidFilePath_ReturnsCorrectHash`
- `ComputeHashAsync_WithNullFilePath_ThrowsArgumentException`
- `ComputeHashAsync_WithEmptyFilePath_ThrowsArgumentException`
- `ComputeHashAsync_WithNonExistentFile_ThrowsFileNotFoundException`
- `ToHexString_WithValidHash_ReturnsCorrectHexString`
- `ToHexString_WithNullHash_ThrowsArgumentNullException`
- `FromHexString_WithValidHex_ReturnsCorrectBytes`
- `FromHexString_WithNullHex_ThrowsArgumentException`
- `FromHexString_WithEmptyHex_ThrowsArgumentException`
- `FromHexString_WithOddLengthHex_ThrowsArgumentException`
- `FromHexString_WithInvalidHexCharacters_ThrowsFormatException`
- `AlgorithmName_ReturnsCorrectValue`
- `HashLength_ReturnsCorrectValue`

### FileStorageServiceTests (Hash Integration)
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Storage/FileStorageServiceTests.cs`
- `CompleteChunkedUploadAsync_ComputesAndStoresFileHash`
- `CompleteChunkedUploadAsync_WithHashServiceFailure_PropagatesException`
- `CompleteChunkedUploadAsync_WithIdenticalFiles_GeneratesSameHash`

### KnowledgeFileRepositoryTests (Hash Support)
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Repositories/KnowledgeFileRepositoryTests.cs`
- `CreateAsync_WithValidHash_StoresHashCorrectly`
- `CreateAsync_WithNullHash_ThrowsArgumentException`
- `CreateAsync_WithEmptyHash_ThrowsArgumentException`
- `GetByIdInKnowledgeBaseAsync_ReturnsFileWithHash`
- `GetByKnowledgeBaseIdAsync_ReturnsFilesWithHashes`
- `GetByKnowledgeBaseIdAndUserIdAsync_ReturnsFilesWithHashes`
- `GetKnowledgeFileByParameterAsync_ReturnsFileWithHash`

### KnowledgeFileDtoTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Models/Dto/KnowledgeFileDtoTests.cs`
- `FromFile_WithValidFile_ConvertsHashToHexCorrectly`
- `FromFile_WithFileContainingNullHash_ThrowsException`
- `Constructor_WithValidHashHex_CreatesCorrectly`
- `HashHex_ReturnsUppercaseHexString`
- `HashHex_WithEmptyHash_ReturnsEmptyString`

### KnowledgeBaseAuthorizationServiceTests (Updated for Hash)
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Auth/KnowledgeBaseAuthorizationServiceTests.cs`
- Existing authorization tests should continue to work with hash-enabled file operations

### Integration Tests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Integration/FileHashIntegrationTests.cs`
- `EndToEndUpload_GeneratesAndStoresCorrectHash`
- `MultipleIdenticalUploads_GenerateIdenticalHashes`
- `FileHashConsistency_AcrossServiceRestarts`

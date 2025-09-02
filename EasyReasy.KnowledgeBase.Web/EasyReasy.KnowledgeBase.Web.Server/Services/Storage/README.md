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
- **Library permissions** - Who can read/write to which libraries?
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
- **Authorization system** - Complete library permission system with read/write/admin levels
- **Database schema** - PostgreSQL enum-based permission types with proper foreign key relationships
- **Service layer security** - All FileStorageService methods now require user authentication and validate permissions
- **File hashing** - SHA-256 hash computation and storage for all uploaded files via `IFileHashService`
- **Hash service abstraction** - Pluggable hashing implementation with `Sha256FileHashService` for consistent hash computation across services
- **Hash service tests** - Comprehensive unit tests for `Sha256FileHashService` with 100% coverage

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

The following test classes and methods should be implemented to ensure proper functionality of the complete file storage and authorization system:

### Authorization System Tests

#### LibraryAuthorizationServiceTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Auth/LibraryAuthorizationServiceTests.cs`
- `ValidateAccessAsync_WithOwner_AllowsAllPermissions`
- `ValidateAccessAsync_WithPublicLibrary_AllowsRead`
- `ValidateAccessAsync_WithPublicLibrary_DeniesWrite`
- `ValidateAccessAsync_WithExplicitReadPermission_AllowsRead`
- `ValidateAccessAsync_WithExplicitWritePermission_AllowsReadAndWrite`
- `ValidateAccessAsync_WithExplicitAdminPermission_AllowsAll`
- `ValidateAccessAsync_WithNoPermission_DeniesAccess`
- `ValidateAccessAsync_WithNonExistentLibrary_ThrowsException`
- `HasAccessAsync_WithOwner_ReturnsTrue`
- `HasAccessAsync_WithPublicRead_ReturnsCorrectAccess`
- `HasAccessAsync_WithExplicitPermissions_ReturnsCorrectAccess`
- `HasAccessAsync_WithNoPermission_ReturnsFalse`

#### LibraryPermissionRepositoryTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Repositories/LibraryPermissionRepositoryTests.cs`
- `HasPermissionAsync_WithOwner_ReturnsTrue`
- `HasPermissionAsync_WithExplicitPermission_ReturnsTrue`
- `HasPermissionAsync_WithInsufficientPermission_ReturnsFalse`
- `HasPermissionAsync_WithNoPermission_ReturnsFalse`
- `GetAccessibleLibraryIdsAsync_ReturnsOwnedLibraries`
- `GetAccessibleLibraryIdsAsync_ReturnsPublicLibraries`
- `GetAccessibleLibraryIdsAsync_ReturnsExplicitPermissions`
- `GrantPermissionAsync_WithValidData_CreatesPermission`
- `GrantPermissionAsync_WithDuplicatePermission_UpdatesPermission`
- `GrantPermissionAsync_WithInvalidLibrary_ThrowsException`
- `RevokePermissionAsync_WithExistingPermission_RemovesPermission`
- `RevokePermissionAsync_WithNonExistentPermission_DoesNotThrow`

#### LibraryRepositoryTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Repositories/LibraryRepositoryTests.cs`
- `GetByIdAsync_WithExistingId_ReturnsLibrary`
- `GetByIdAsync_WithNonExistentId_ReturnsNull`
- `CreateAsync_WithValidData_CreatesLibrary`
- `CreateAsync_WithInvalidData_ThrowsException`
- `UpdateAsync_WithValidData_UpdatesLibrary`
- `UpdateAsync_WithNonExistentId_ThrowsException`
- `DeleteAsync_WithExistingId_DeletesLibrary`
- `DeleteAsync_WithNonExistentId_DoesNotThrow`
- `GetByOwnerIdAsync_ReturnsOwnedLibraries`
- `GetPublicLibrariesAsync_ReturnsOnlyPublicLibraries`

### File Storage System Tests

#### FileStorageServiceTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Storage/FileStorageServiceTests.cs`

**Chunked Upload Tests:**
- `InitiateChunkedUploadAsync_WithValidData_CreatesSession`
- `InitiateChunkedUploadAsync_WithoutWritePermission_ThrowsUnauthorizedException`
- `InitiateChunkedUploadAsync_WithFileTooLarge_ThrowsException`
- `UploadChunkAsync_WithValidChunk_UploadsSuccessfully`
- `UploadChunkAsync_WithInvalidSession_ThrowsException`
- `UploadChunkAsync_WithChunkTooLarge_ThrowsException`
- `CompleteChunkedUploadAsync_WithValidSession_CompletesUpload`
- `CompleteChunkedUploadAsync_WithIncompleteChunks_ThrowsException`
- `CompleteChunkedUploadAsync_ComputesAndStoresFileHash`
- `CompleteChunkedUploadAsync_WithHashServiceFailure_PropagatesException`
- `CancelChunkedUploadAsync_WithValidSession_CancelsUpload`

**File Operations Tests:**
- `GetFileInfoAsync_WithValidFile_ReturnsFileInfo`
- `GetFileInfoAsync_WithoutReadPermission_ThrowsUnauthorizedException`
- `GetFileInfoAsync_WithNonExistentFile_ReturnsNull`
- `GetFileStreamAsync_WithValidFile_ReturnsStream`
- `GetFileStreamAsync_WithoutReadPermission_ThrowsUnauthorizedException`
- `GetFileContentAsync_WithValidFile_ReturnsContent`
- `DeleteFileAsync_WithValidFile_DeletesFile`
- `DeleteFileAsync_WithoutWritePermission_ThrowsUnauthorizedException`
- `ListFilesAsync_WithValidLibrary_ReturnsFiles`
- `ListFilesAsync_WithoutReadPermission_ThrowsUnauthorizedException`
- `FileExistsAsync_WithExistingFile_ReturnsTrue`
- `FileExistsAsync_WithNonExistentFile_ReturnsFalse`
- `DeleteLibraryAsync_WithValidLibrary_DeletesAllFiles`
- `DeleteLibraryAsync_WithoutAdminPermission_ThrowsUnauthorizedException`

#### ✅ Sha256FileHashServiceTests (COMPLETED)
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Services/Hashing/Sha256FileHashServiceTests.cs`
- ✅ `ComputeHashAsync_WithValidStream_ReturnsCorrectHash`
- ✅ `ComputeHashAsync_WithNullStream_ThrowsArgumentNullException`
- ✅ `ComputeHashAsync_WithValidFilePath_ReturnsCorrectHash`
- ✅ `ComputeHashAsync_WithNullFilePath_ThrowsArgumentException`
- ✅ `ComputeHashAsync_WithEmptyFilePath_ThrowsArgumentException`
- ✅ `ComputeHashAsync_WithNonExistentFile_ThrowsFileNotFoundException`
- ✅ `ToHexString_WithValidHash_ReturnsCorrectHexString`
- ✅ `ToHexString_WithNullHash_ThrowsArgumentNullException`
- ✅ `FromHexString_WithValidHex_ReturnsCorrectBytes`
- ✅ `FromHexString_WithNullHex_ThrowsArgumentException`
- ✅ `FromHexString_WithEmptyHex_ThrowsArgumentException`
- ✅ `FromHexString_WithOddLengthHex_ThrowsArgumentException`
- ✅ `FromHexString_WithInvalidHexCharacters_ThrowsFormatException`
- ✅ `AlgorithmName_ReturnsCorrectValue`
- ✅ `HashLength_ReturnsCorrectValue`

### Repository Tests

#### LibraryFileRepositoryTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Repositories/LibraryFileRepositoryTests.cs`
- `CreateAsync_WithValidData_CreatesFile`
- `CreateAsync_WithNullHash_ThrowsArgumentException`
- `CreateAsync_WithEmptyHash_ThrowsArgumentException`
- `CreateAsync_WithInvalidLibraryId_ThrowsException`
- `CreateAsync_WithDuplicateRelativePath_ThrowsException`
- `GetByIdInLibraryAsync_WithExistingFile_ReturnsFile`
- `GetByIdInLibraryAsync_WithNonExistentFile_ReturnsNull`
- `GetByIdInLibraryAsync_WithWrongLibrary_ReturnsNull`
- `GetByLibraryIdAsync_WithFiles_ReturnsAllFiles`
- `GetByLibraryIdAsync_WithNoFiles_ReturnsEmptyList`
- `GetByLibraryIdAndUserIdAsync_WithUserFiles_ReturnsUserFiles`
- `GetLibraryFileByParameterAsync_WithValidParameter_ReturnsFile`
- `DeleteAsync_WithExistingFile_DeletesFile`
- `DeleteAsync_WithNonExistentFile_DoesNotThrow`

### Model and DTO Tests

#### LibraryTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Models/LibraryTests.cs`
- `Constructor_WithValidData_CreatesInstance`
- `Constructor_WithNullValues_ThrowsArgumentException`
- `Constructor_WithEmptyStrings_ThrowsArgumentException`

#### LibraryPermissionTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Models/LibraryPermissionTests.cs`
- `Constructor_WithValidData_CreatesInstance`
- `Constructor_WithInvalidPermissionType_ThrowsException`

#### LibraryFileTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Models/LibraryFileTests.cs`
- `Constructor_WithValidData_CreatesInstance`
- `Constructor_WithNullHash_ThrowsArgumentException`
- `Constructor_WithEmptyHash_ThrowsArgumentException`
- `Constructor_WithNegativeSize_ThrowsArgumentException`

#### LibraryFileDtoTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Models/Dto/LibraryFileDtoTests.cs`
- `FromFile_WithValidFile_ConvertsHashToHexCorrectly`
- `FromFile_WithFileContainingNullHash_ThrowsException`
- `Constructor_WithValidHashHex_CreatesCorrectly`
- `HashHex_ReturnsUppercaseHexString`

### Controller Tests

#### FileStorageControllerTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Controllers/FileStorageControllerTests.cs`
- `InitiateChunkedUpload_WithValidRequest_ReturnsSession`
- `InitiateChunkedUpload_WithoutAuthentication_ReturnsUnauthorized`
- `UploadChunk_WithValidChunk_ReturnsSuccess`
- `UploadChunk_WithInvalidSession_ReturnsBadRequest`
- `CompleteChunkedUpload_WithValidSession_ReturnsFileDto`
- `CompleteChunkedUpload_WithIncompleteUpload_ReturnsBadRequest`
- `GetFileInfo_WithValidFile_ReturnsFileDto`
- `GetFileInfo_WithoutPermission_ReturnsForbidden`
- `DownloadFile_WithValidFile_ReturnsFileStream`
- `DownloadFile_WithoutPermission_ReturnsForbidden`
- `DeleteFile_WithValidFile_ReturnsSuccess`
- `DeleteFile_WithoutPermission_ReturnsForbidden`
- `ListFiles_WithValidLibrary_ReturnsFileList`
- `ListFiles_WithoutPermission_ReturnsForbidden`

### Integration Tests

#### FileStorageIntegrationTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Integration/FileStorageIntegrationTests.cs`
- `EndToEndUpload_WithAuthorizedUser_CompletesSuccessfully`
- `EndToEndUpload_WithUnauthorizedUser_Fails`
- `MultipleChunkedUploads_CompleteIndependently`
- `FileHashConsistency_AcrossServiceRestarts`
- `PermissionChanges_ReflectedInFileAccess`
- `LibraryPermissions_EnforceFileAccess`

#### AuthorizationIntegrationTests
**Location**: `EasyReasy.KnowledgeBase.Web.Server.Tests/Integration/AuthorizationIntegrationTests.cs`
- `OwnerAccess_AllowsAllOperations`
- `PublicLibrary_AllowsReadOnlyAccess`
- `ExplicitPermissions_EnforceCorrectAccess`
- `PermissionInheritance_WorksCorrectly`
- `MultiUserScenarios_IsolateAccess`

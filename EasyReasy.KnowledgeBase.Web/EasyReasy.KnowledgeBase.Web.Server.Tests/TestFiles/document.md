# Integration Test Document

This is a **markdown document** used for testing file upload and storage functionality in the FileStorageController integration tests.

## Features Tested

- Chunked file uploads
- File storage and retrieval
- Content type handling
- Authorization flows

### File Upload Flow

1. **Initiate**: Start a chunked upload session
2. **Upload**: Send file chunks sequentially
3. **Complete**: Finalize the upload process
4. **Verify**: Confirm file was stored correctly

### Sample Code Block

```csharp
// Example of uploading a file
var response = await client.PostAsync("/api/filestorage/initiate-chunked-upload", content);
```

## Test Scenarios

- **Small Files**: Basic upload functionality
- **Large Files**: Multi-chunk upload handling  
- **Different Types**: JSON, text, markdown files
- **Error Cases**: Invalid chunks, expired sessions

> **Note**: This document provides enough content to test various chunking scenarios while remaining readable and maintainable.

## Conclusion

This markdown file serves as test data for comprehensive integration testing of the file storage system, ensuring robust handling of different file types and upload patterns.

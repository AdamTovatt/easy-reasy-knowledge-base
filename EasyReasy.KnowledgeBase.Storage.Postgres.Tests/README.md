# PostgreSQL Storage Tests

This test project contains integration tests for the PostgreSQL storage implementation of the EasyReasy Knowledge Base system.

## Prerequisites

- PostgreSQL server running and accessible
- .NET 8.0 SDK
- Test database with appropriate permissions

## Configuration

The tests use environment variables for configuration, similar to the Ollama integration tests. Follow these steps to set up:

### 1. Update the Environment Variables File

The `TestEnvironmentVariables.txt` file is located in the test project root directory. Update it with your actual PostgreSQL server details:

### 2. Update Configuration Values

Edit the `TestEnvironmentVariables.txt` file with your actual PostgreSQL connection string:

```txt
# PostgreSQL Integration Test Configuration
# Update this file with your actual PostgreSQL server details

# The full PostgreSQL connection string for testing
POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=test_knowledgebase;Username=postgres;Password=your-actual-password
```

### 3. Create Test Database

Ensure the test database exists and is accessible:

```sql
CREATE DATABASE test_knowledgebase;
```

## Running Tests

The tests will automatically:

1. Load environment variables from `TestEnvironmentVariables.txt` (assembly-level initialization)
2. Validate that all required variables are present
3. Set up the database schema using DbUp migrations
4. Run the tests with a clean database state for each test
5. Clean up after each test

### Test Structure

- **AssemblyInitialize**: Loads environment variables once for the entire test assembly
- **ClassInitialize**: Sets up the database connection and services
- **TestInitialize**: Ensures a clean database state before each test
- **TestCleanup**: Cleans up after each test
- **ClassCleanup**: Final cleanup when all tests are complete

## Database Migrations

The tests use DbUp to manage database schema. The migration script `001_InitialSchema.sql` is embedded in the test assembly and will be automatically applied during test setup.

## Troubleshooting

### Common Issues

1. **"Could not load TestEnvironmentVariables.txt"**
   - Ensure the file exists in the test project root directory
   - Check file permissions and path

2. **"Failed to initialize PostgreSQL test environment"**
   - Verify PostgreSQL server is running
   - Check connection string parameters
   - Ensure database exists and user has appropriate permissions

3. **Database connection errors**
   - Verify the connection string format and parameters
   - Check PostgreSQL server logs
   - Ensure the test database exists

### Debug Information

The tests include detailed logging that will help diagnose issues. Check the test output for:
- Environment variable loading status
- Database connection attempts
- Migration execution details
- Test execution timing

## Test Categories

The tests cover:

- **File Store Tests**: CRUD operations for knowledge files
- **Section Store Tests**: CRUD operations for file sections
- **Chunk Store Tests**: CRUD operations for file chunks
- **Knowledge Store Tests**: Integration tests for the complete storage system

Each test category includes:
- Constructor validation
- Basic CRUD operations
- Edge cases and error conditions
- Performance considerations

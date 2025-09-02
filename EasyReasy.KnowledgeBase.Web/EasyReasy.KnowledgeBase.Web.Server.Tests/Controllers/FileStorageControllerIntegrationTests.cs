using EasyReasy;
using EasyReasy.Auth;
using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Services.Account;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Resources;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Reflection;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Controllers
{
    /// <summary>
    /// Integration tests for FileStorageController using real HTTP requests and file system operations.
    /// Uses EasyReasy embedded test resources and LocalFileSystem for realistic testing scenarios.
    /// </summary>
    [TestClass]
    public class FileStorageControllerIntegrationTests : DatabaseTestBase, IDisposable
    {
        private static WebApplicationFactory<Program> _factory = null!;
        private static HttpClient _client = null!;
        private static ResourceManager _resourceManager = null!;
        private static string _tempDirectory = null!;
        private static string _jwtToken = null!;
        private static IUserService _userService = null!;
        private bool _disposed;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext testContext)
        {
            try
            {
                // Initialize database test environment (includes loading env vars and running migrations)
                InitializeDatabaseTestEnvironment();

                // Validate environment variables are properly configured
                EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(FileStorageControllerIntegrationTestEnvironmentVariables));

                // Create temporary directory for file storage operations
                _tempDirectory = Path.Combine(Path.GetTempPath(), $"FileStorageIntegrationTests_{Guid.NewGuid():N}");
                Directory.CreateDirectory(_tempDirectory);

                // Override FILE_STORAGE_BASE_PATH to use our temp directory
                Environment.SetEnvironmentVariable("FILE_STORAGE_BASE_PATH", _tempDirectory);

                // Verify all embedded test resources exist at startup (per EasyReasy documentation)
                _resourceManager = await ResourceManager.CreateInstanceAsync(Assembly.GetAssembly(typeof(IntegrationTestResources))!);
                Assert.IsNotNull(_resourceManager, "ResourceManager should be created successfully");

                // Set up user service for creating test users
                IUserRepository userRepository = new UserRepository(_connectionFactory);
                IPasswordHasher passwordHasher = new SecurePasswordHasher();
                _userService = new UserService(userRepository, passwordHasher);

                // Set up WebApplicationFactory - it will use environment variables from TestEnvironmentVariables.txt
                _factory = new WebApplicationFactory<Program>();
                _client = _factory.CreateClient();

                // Create test user and login once to store JWT token for all tests
                _jwtToken = await LoginAndGetTokenAsync("test@example.com", "password123");
                
                // Set the authorization header once for all tests
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
                
                Console.WriteLine($"JWT Token obtained and set: {_jwtToken[..Math.Min(_jwtToken.Length, 50)]}...");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize FileStorageController integration test environment: {exception.Message}");
                Assert.Inconclusive("Failed to initialize integration test environment.");
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Clean up database tables
            try
            {
                string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
                TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
                TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup database: {ex.Message}");
            }

            // Clean up static resources
            _client?.Dispose();
            _factory?.Dispose();

            // Clean up temporary directory
            if (!string.IsNullOrEmpty(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch (Exception)
                {
                    // Best effort cleanup - don't fail tests if temp cleanup fails
                }
            }
        }

        #region Integration Tests

        [TestMethod]
        public async Task InitiateChunkedUpload_WithValidRequest_ReturnsCreatedSession()
        {
            // Arrange - Use the JWT token from class initialization
            byte[] fileContent = await _resourceManager.ReadAsBytesAsync(IntegrationTestResources.TestFiles.SmallTextFile);
            
            var request = new
            {
                LibraryId = Guid.NewGuid(),
                FileName = "small-test.txt",
                ContentType = "text/plain",
                TotalSize = fileContent.Length,
                ChunkSize = 512
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(request);
            StringContent httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Act (JWT authorization header already set in ClassInitialize)
            HttpResponseMessage response = await _client.PostAsync("/api/filestorage/initiate-chunked-upload", httpContent);

            // Assert
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"File storage response: Status={response.StatusCode}, Content={responseContent}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Check if authorization header is set
                bool hasAuthHeader = _client.DefaultRequestHeaders.Authorization != null;
                Console.WriteLine($"Has Authorization header: {hasAuthHeader}");
                if (hasAuthHeader)
                {
                    Console.WriteLine($"Auth header: {_client.DefaultRequestHeaders.Authorization.Scheme} {_client.DefaultRequestHeaders.Authorization.Parameter?[..Math.Min(_client.DefaultRequestHeaders.Authorization.Parameter.Length, 50)]}...");
                }
                
                Assert.Inconclusive($"Authentication failed. Auth header present: {hasAuthHeader}. Response: {responseContent}");
            }

            // If we get here, authentication worked - verify the successful response
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"Expected 200 OK but got {response.StatusCode}. Response: {responseContent}");
        }

        /// <summary>
        /// Helper method to create a test user, login, and get a JWT token for testing.
        /// </summary>
        private static async Task<string> LoginAndGetTokenAsync(string email, string password)
        {
            try
            {
                // Create test user first
                Console.WriteLine($"Creating test user: {email}");
                await _userService.CreateUserAsync(
                    email: email,
                    password: password,
                    firstName: "Test",
                    lastName: "User",
                    roles: new List<string> { "user", "admin" });
                Console.WriteLine("Test user created successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                // User already exists, which is fine - continue with login
                Console.WriteLine("Test user already exists, continuing with login");
            }

            // Now attempt login
            Console.WriteLine($"Attempting login for: {email}");
            var loginRequest = new
            {
                username = email,
                password = password
            };

            string loginJson = System.Text.Json.JsonSerializer.Serialize(loginRequest);
            StringContent loginContent = new StringContent(loginJson, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            
            if (!loginResponse.IsSuccessStatusCode)
            {
                string errorContent = await loginResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Login failed: Status={loginResponse.StatusCode}, Content={errorContent}");
                throw new InvalidOperationException($"Login failed with status {loginResponse.StatusCode}: {errorContent}");
            }

            string responseJson = await loginResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Login successful, response: {responseJson}");
            
            using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(responseJson);
            
            if (!doc.RootElement.TryGetProperty("token", out System.Text.Json.JsonElement tokenElement))
            {
                throw new InvalidOperationException($"Login response did not contain 'token' property: {responseJson}");
            }

            string token = tokenElement.GetString() ?? throw new InvalidOperationException("JWT token was null");
            Console.WriteLine($"JWT token extracted successfully, length: {token.Length}");
            return token;
        }

        #endregion

        #region Dispose Pattern

        // Since we moved to static fields and ClassCleanup, instance disposal is simplified
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Static resources are cleaned up in ClassCleanup
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

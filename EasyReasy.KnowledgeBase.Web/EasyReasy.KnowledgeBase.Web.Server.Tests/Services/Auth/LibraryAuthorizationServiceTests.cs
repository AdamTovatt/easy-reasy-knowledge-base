using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Services.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Services.Auth
{
    /// <summary>
    /// Unit tests for the LibraryAuthorizationService using mocked dependencies.
    /// </summary>
    [TestClass]
    public class LibraryAuthorizationServiceTests
    {
        private Mock<ILibraryRepository> _mockLibraryRepository = null!;
        private Mock<ILibraryPermissionRepository> _mockPermissionRepository = null!;
        private Mock<ILogger<LibraryAuthorizationService>> _mockLogger = null!;
        private LibraryAuthorizationService _authorizationService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLibraryRepository = new Mock<ILibraryRepository>();
            _mockPermissionRepository = new Mock<ILibraryPermissionRepository>();
            _mockLogger = new Mock<ILogger<LibraryAuthorizationService>>();

            _authorizationService = new LibraryAuthorizationService(
                _mockLibraryRepository.Object,
                _mockPermissionRepository.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullLibraryRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new LibraryAuthorizationService(null!, _mockPermissionRepository.Object, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullPermissionRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new LibraryAuthorizationService(_mockLibraryRepository.Object, null!, _mockLogger.Object));
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new LibraryAuthorizationService(_mockLibraryRepository.Object, _mockPermissionRepository.Object, null!));
        }

        [TestMethod]
        public void Constructor_WithValidArguments_CreatesInstance()
        {
            // Act
            LibraryAuthorizationService service = new LibraryAuthorizationService(
                _mockLibraryRepository.Object, _mockPermissionRepository.Object, _mockLogger.Object);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region HasPermissionAsync Tests

        [TestMethod]
        public async Task HasPermissionAsync_WithValidRequest_ReturnsRepositoryResult()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            LibraryPermissionType permissionType = LibraryPermissionType.Read;

            _mockPermissionRepository
                .Setup(r => r.HasPermissionAsync(userId, libraryId, permissionType))
                .ReturnsAsync(true);

            // Act
            bool result = await _authorizationService.HasPermissionAsync(userId, libraryId, permissionType);

            // Assert
            Assert.IsTrue(result);
            _mockPermissionRepository.Verify(r => r.HasPermissionAsync(userId, libraryId, permissionType), Times.Once);
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithRepositoryException_ReturnsFalseAndLogs()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            LibraryPermissionType permissionType = LibraryPermissionType.Write;
            Exception testException = new Exception("Database error");

            _mockPermissionRepository
                .Setup(r => r.HasPermissionAsync(userId, libraryId, permissionType))
                .ThrowsAsync(testException);

            // Act
            bool result = await _authorizationService.HasPermissionAsync(userId, libraryId, permissionType);

            // Assert
            Assert.IsFalse(result); // Fail closed
            VerifyLoggerError(_mockLogger, "Error checking permission");
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithAllPermissionTypes_CallsRepositoryCorrectly()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            LibraryPermissionType[] permissionTypes = { LibraryPermissionType.Read, LibraryPermissionType.Write, LibraryPermissionType.Admin };

            foreach (LibraryPermissionType permissionType in permissionTypes)
            {
                _mockPermissionRepository
                    .Setup(r => r.HasPermissionAsync(userId, libraryId, permissionType))
                    .ReturnsAsync(true);

                // Act
                bool result = await _authorizationService.HasPermissionAsync(userId, libraryId, permissionType);

                // Assert
                Assert.IsTrue(result, $"Should return true for {permissionType}");
                _mockPermissionRepository.Verify(r => r.HasPermissionAsync(userId, libraryId, permissionType), Times.Once);
            }
        }

        #endregion

        #region GetEffectivePermissionAsync Tests

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithOwner_ReturnsAdmin()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(true);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Admin, result);
            _mockLibraryRepository.Verify(r => r.IsOwnerAsync(libraryId, userId), Times.Once);
            // Should not call other repository methods when owner is determined
            _mockLibraryRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithNonExistentLibrary_ReturnsNull()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync((Library?)null);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.IsNull(result);
            _mockLibraryRepository.Verify(r => r.IsOwnerAsync(libraryId, userId), Times.Once);
            _mockLibraryRepository.Verify(r => r.GetByIdAsync(libraryId), Times.Once);
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithExplicitPermission_ReturnsExplicitPermission()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: false);

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(userId, libraryId))
                .ReturnsAsync(LibraryPermissionType.Write);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Write, result);
            _mockPermissionRepository.Verify(r => r.GetUserPermissionAsync(userId, libraryId), Times.Once);
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithPublicLibraryNoExplicitPermission_ReturnsRead()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: true);

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(userId, libraryId))
                .ReturnsAsync((LibraryPermissionType?)null);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Read, result);
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithPrivateLibraryNoExplicitPermission_ReturnsNull()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: false);

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(userId, libraryId))
                .ReturnsAsync((LibraryPermissionType?)null);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_WithException_ReturnsNullAndLogs()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Exception testException = new Exception("Database error");

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ThrowsAsync(testException);

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.IsNull(result); // Fail closed
            VerifyLoggerError(_mockLogger, "Error getting effective permission");
        }

        [TestMethod]
        public async Task GetEffectivePermissionAsync_ExplicitPermissionOverridesPublic_ReturnsExplicitPermission()
        {
            // Arrange - User has explicit read permission on public library
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: true);

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(userId, libraryId))
                .ReturnsAsync(LibraryPermissionType.Admin); // Higher than public read

            // Act
            LibraryPermissionType? result = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Admin, result); // Should return explicit, not public read
        }

        #endregion

        #region GetAccessibleKnowledgeBaseIdsAsync Tests

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithValidRequest_ReturnsRepositoryResult()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            LibraryPermissionType minimumPermission = LibraryPermissionType.Read;
            List<Guid> expectedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _mockPermissionRepository
                .Setup(r => r.GetAccessibleLibraryIdsAsync(userId, minimumPermission))
                .ReturnsAsync(expectedIds);

            // Act
            List<Guid> result = await _authorizationService.GetAccessibleKnowledgeBaseIdsAsync(userId, minimumPermission);

            // Assert
            CollectionAssert.AreEqual(expectedIds, result);
            _mockPermissionRepository.Verify(r => r.GetAccessibleLibraryIdsAsync(userId, minimumPermission), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithDefaultParameter_UsesReadPermission()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            List<Guid> expectedIds = new List<Guid> { Guid.NewGuid() };

            _mockPermissionRepository
                .Setup(r => r.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Read))
                .ReturnsAsync(expectedIds);

            // Act
            List<Guid> result = await _authorizationService.GetAccessibleKnowledgeBaseIdsAsync(userId);

            // Assert
            CollectionAssert.AreEqual(expectedIds, result);
            _mockPermissionRepository.Verify(r => r.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Read), Times.Once);
        }

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithException_ReturnsEmptyListAndLogs()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            LibraryPermissionType minimumPermission = LibraryPermissionType.Write;
            Exception testException = new Exception("Database error");

            _mockPermissionRepository
                .Setup(r => r.GetAccessibleLibraryIdsAsync(userId, minimumPermission))
                .ThrowsAsync(testException);

            // Act
            List<Guid> result = await _authorizationService.GetAccessibleKnowledgeBaseIdsAsync(userId, minimumPermission);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            VerifyLoggerError(_mockLogger, "Error getting accessible libraries");
        }

        #endregion

        #region ValidateAccessAsync Tests

        [TestMethod]
        public async Task ValidateAccessAsync_WithSufficientPermission_DoesNotThrow()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            LibraryPermissionType requiredPermission = LibraryPermissionType.Read;
            string actionDescription = "read files";

            _mockPermissionRepository
                .Setup(r => r.HasPermissionAsync(userId, libraryId, requiredPermission))
                .ReturnsAsync(true);

            // Act & Assert - Should not throw
            await _authorizationService.ValidateAccessAsync(userId, libraryId, requiredPermission, actionDescription);

            _mockPermissionRepository.Verify(r => r.HasPermissionAsync(userId, libraryId, requiredPermission), Times.Once);
        }

        [TestMethod]
        public async Task ValidateAccessAsync_WithInsufficientPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            LibraryPermissionType requiredPermission = LibraryPermissionType.Write;
            string actionDescription = "upload files";

            _mockPermissionRepository
                .Setup(r => r.HasPermissionAsync(userId, libraryId, requiredPermission))
                .ReturnsAsync(false);

            // Act & Assert
            UnauthorizedAccessException exception = await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _authorizationService.ValidateAccessAsync(userId, libraryId, requiredPermission, actionDescription));

            Assert.IsTrue(exception.Message.Contains("Write permission required"));
            Assert.IsTrue(exception.Message.Contains("upload files"));
            VerifyLoggerWarning(_mockLogger, "Access denied");
        }

        [TestMethod]
        public async Task ValidateAccessAsync_WithAllPermissionTypes_HandlesCorrectly()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            
            var testCases = new[]
            {
                new { Permission = LibraryPermissionType.Read, Action = "view files", HasAccess = true },
                new { Permission = LibraryPermissionType.Write, Action = "upload files", HasAccess = false },
                new { Permission = LibraryPermissionType.Admin, Action = "delete library", HasAccess = true }
            };

            foreach (var testCase in testCases)
            {
                _mockPermissionRepository
                    .Setup(r => r.HasPermissionAsync(userId, libraryId, testCase.Permission))
                    .ReturnsAsync(testCase.HasAccess);

                if (testCase.HasAccess)
                {
                    // Act & Assert - Should not throw
                    await _authorizationService.ValidateAccessAsync(userId, libraryId, testCase.Permission, testCase.Action);
                }
                else
                {
                    // Act & Assert - Should throw
                    await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                        () => _authorizationService.ValidateAccessAsync(userId, libraryId, testCase.Permission, testCase.Action));
                }
            }
        }

        #endregion

        #region IsOwnerAsync Tests

        [TestMethod]
        public async Task IsOwnerAsync_WithValidRequest_ReturnsRepositoryResult()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(true);

            // Act
            bool result = await _authorizationService.IsOwnerAsync(userId, libraryId);

            // Assert
            Assert.IsTrue(result);
            _mockLibraryRepository.Verify(r => r.IsOwnerAsync(libraryId, userId), Times.Once);
        }

        [TestMethod]
        public async Task IsOwnerAsync_WithNonOwner_ReturnsFalse()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ReturnsAsync(false);

            // Act
            bool result = await _authorizationService.IsOwnerAsync(userId, libraryId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsOwnerAsync_WithException_ReturnsFalseAndLogs()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Exception testException = new Exception("Database error");

            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, userId))
                .ThrowsAsync(testException);

            // Act
            bool result = await _authorizationService.IsOwnerAsync(userId, libraryId);

            // Assert
            Assert.IsFalse(result); // Fail closed
            VerifyLoggerError(_mockLogger, "Error checking ownership");
        }

        #endregion

        #region IsPublicAsync Tests

        [TestMethod]
        public async Task IsPublicAsync_WithPublicLibrary_ReturnsTrue()
        {
            // Arrange
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: true);

            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);

            // Act
            bool result = await _authorizationService.IsPublicAsync(libraryId);

            // Assert
            Assert.IsTrue(result);
            _mockLibraryRepository.Verify(r => r.GetByIdAsync(libraryId), Times.Once);
        }

        [TestMethod]
        public async Task IsPublicAsync_WithPrivateLibrary_ReturnsFalse()
        {
            // Arrange
            Guid libraryId = Guid.NewGuid();
            Library library = CreateTestLibrary(libraryId, isPublic: false);

            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);

            // Act
            bool result = await _authorizationService.IsPublicAsync(libraryId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsPublicAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            Guid libraryId = Guid.NewGuid();

            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync((Library?)null);

            // Act
            bool result = await _authorizationService.IsPublicAsync(libraryId);

            // Assert
            Assert.IsFalse(result); // Fail closed for non-existent library
        }

        [TestMethod]
        public async Task IsPublicAsync_WithException_ReturnsFalseAndLogs()
        {
            // Arrange
            Guid libraryId = Guid.NewGuid();
            Exception testException = new Exception("Database error");

            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ThrowsAsync(testException);

            // Act
            bool result = await _authorizationService.IsPublicAsync(libraryId);

            // Assert
            Assert.IsFalse(result); // Fail closed
            VerifyLoggerError(_mockLogger, "Error checking if library");
        }

        #endregion

        #region Integration and Complex Scenario Tests

        [TestMethod]
        public async Task ComplexAuthorizationScenario_WorksCorrectly()
        {
            // Arrange - Create a scenario with multiple users and permission levels
            Guid ownerId = Guid.NewGuid();
            Guid readUserId = Guid.NewGuid();
            Guid writeUserId = Guid.NewGuid();
            Guid publicUserId = Guid.NewGuid();
            Guid unauthorizedUserId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();

            Library library = CreateTestLibrary(libraryId, isPublic: true);

            // Setup owner
            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, ownerId))
                .ReturnsAsync(true);

            // Setup explicit permissions
            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(libraryId, It.Is<Guid>(id => id != ownerId)))
                .ReturnsAsync(false);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(libraryId))
                .ReturnsAsync(library);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(readUserId, libraryId))
                .ReturnsAsync(LibraryPermissionType.Read);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(writeUserId, libraryId))
                .ReturnsAsync(LibraryPermissionType.Write);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(publicUserId, libraryId))
                .ReturnsAsync((LibraryPermissionType?)null);
            _mockPermissionRepository
                .Setup(r => r.GetUserPermissionAsync(unauthorizedUserId, libraryId))
                .ReturnsAsync((LibraryPermissionType?)null);

            // Act & Assert - Test various permission scenarios
            // Owner should always have admin
            LibraryPermissionType? ownerPermission = await _authorizationService.GetEffectivePermissionAsync(ownerId, libraryId);
            Assert.AreEqual(LibraryPermissionType.Admin, ownerPermission);

            // User with explicit read permission should have read
            LibraryPermissionType? readPermission = await _authorizationService.GetEffectivePermissionAsync(readUserId, libraryId);
            Assert.AreEqual(LibraryPermissionType.Read, readPermission);

            // User with explicit write permission should have write
            LibraryPermissionType? writePermission = await _authorizationService.GetEffectivePermissionAsync(writeUserId, libraryId);
            Assert.AreEqual(LibraryPermissionType.Write, writePermission);

            // User with no explicit permission on public library should have read
            LibraryPermissionType? publicPermission = await _authorizationService.GetEffectivePermissionAsync(publicUserId, libraryId);
            Assert.AreEqual(LibraryPermissionType.Read, publicPermission);
        }

        [TestMethod]
        public async Task ErrorHandling_AllMethodsFailClosed()
        {
            // Arrange - Setup all methods to throw exceptions
            Guid userId = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Exception testException = new Exception("Database connection failed");

            _mockPermissionRepository
                .Setup(r => r.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LibraryPermissionType>()))
                .ThrowsAsync(testException);
            _mockLibraryRepository
                .Setup(r => r.IsOwnerAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ThrowsAsync(testException);
            _mockLibraryRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(testException);
            _mockPermissionRepository
                .Setup(r => r.GetAccessibleLibraryIdsAsync(It.IsAny<Guid>(), It.IsAny<LibraryPermissionType>()))
                .ThrowsAsync(testException);

            // Act & Assert - All methods should fail closed (deny access)
            bool hasPermission = await _authorizationService.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read);
            Assert.IsFalse(hasPermission);

            LibraryPermissionType? effectivePermission = await _authorizationService.GetEffectivePermissionAsync(userId, libraryId);
            Assert.IsNull(effectivePermission);

            bool isOwner = await _authorizationService.IsOwnerAsync(userId, libraryId);
            Assert.IsFalse(isOwner);

            bool isPublic = await _authorizationService.IsPublicAsync(libraryId);
            Assert.IsFalse(isPublic);

            List<Guid> accessibleIds = await _authorizationService.GetAccessibleKnowledgeBaseIdsAsync(userId);
            Assert.IsNotNull(accessibleIds);
            Assert.AreEqual(0, accessibleIds.Count);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test library for use in tests.
        /// </summary>
        private static Library CreateTestLibrary(Guid libraryId, bool isPublic = false)
        {
            return new Library(
                id: libraryId,
                name: "Test Library",
                description: "A test library",
                ownerId: Guid.NewGuid(),
                isPublic: isPublic,
                createdAt: DateTime.UtcNow.AddDays(-1),
                updatedAt: DateTime.UtcNow
            );
        }

        /// <summary>
        /// Verifies that an error was logged with the expected message fragment.
        /// </summary>
        private static void VerifyLoggerError(Mock<ILogger<LibraryAuthorizationService>> mockLogger, string expectedMessageFragment)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessageFragment)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Verifies that a warning was logged with the expected message fragment.
        /// </summary>
        private static void VerifyLoggerWarning(Mock<ILogger<LibraryAuthorizationService>> mockLogger, string expectedMessageFragment)
        {
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessageFragment)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}

using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.Helpers;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Repositories
{
    /// <summary>
    /// Integration tests for the LibraryPermissionRepository using a real PostgreSQL database.
    /// </summary>
    [TestClass]
    public class LibraryPermissionRepositoryTests : DatabaseTestBase
    {
        private static ILibraryPermissionRepository _permissionRepository = null!;
        private static ILibraryRepository _libraryRepository = null!;
        private static IUserRepository _userRepository = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            InitializeDatabaseTestEnvironment();
            _permissionRepository = new LibraryPermissionRepository(_connectionFactory);
            _libraryRepository = new LibraryRepository(_connectionFactory);
            _userRepository = new UserRepository(_connectionFactory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test data after each test (order matters due to foreign keys)
            string connectionString = TestEnvironmentVariables.PostgresConnectionString.GetValue();
            TestDatabaseMigrator.CleanupTable(connectionString, "library_permission", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "library", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "user_role", _logger);
            TestDatabaseMigrator.CleanupTable(connectionString, "\"user\"", _logger);
        }

        #region HasPermissionAsync Tests

        [TestMethod]
        public async Task HasPermissionAsync_WithOwner_ReturnsTrue()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, _) = await CreateTestLibraryAndUsers();

            // Act & Assert - Owner should have all permissions
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(ownerId, libraryId, LibraryPermissionType.Read));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(ownerId, libraryId, LibraryPermissionType.Write));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(ownerId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithPublicLibraryRead_ReturnsTrue()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers(isPublic: true);

            // Act & Assert
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Write));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithPrivateLibraryNoPermission_ReturnsFalse()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers(isPublic: false);

            // Act & Assert
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Write));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithExplicitReadPermission_ReturnsCorrectAccess()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers(isPublic: false);
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);

            // Act & Assert
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Write));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithExplicitWritePermission_ReturnsCorrectAccess()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers(isPublic: false);
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Write, ownerId);

            // Act & Assert
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Write));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithExplicitAdminPermission_ReturnsCorrectAccess()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers(isPublic: false);
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Admin, ownerId);

            // Act & Assert - Admin permission should grant all access levels
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Read));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Write));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, libraryId, LibraryPermissionType.Admin));
        }

        [TestMethod]
        public async Task HasPermissionAsync_WithNonExistentLibrary_ReturnsFalse()
        {
            // Arrange
            Guid userId = await CreateTestUser();
            Guid nonExistentLibraryId = Guid.NewGuid();

            // Act & Assert
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, nonExistentLibraryId, LibraryPermissionType.Read));
        }

        #endregion

        #region GetUserPermissionAsync Tests

        [TestMethod]
        public async Task GetUserPermissionAsync_WithExplicitPermission_ReturnsPermissionType()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Write, ownerId);

            // Act
            LibraryPermissionType? permission = await _permissionRepository.GetUserPermissionAsync(userId, libraryId);

            // Assert
            Assert.IsNotNull(permission);
            Assert.AreEqual(LibraryPermissionType.Write, permission.Value);
        }

        [TestMethod]
        public async Task GetUserPermissionAsync_WithNoExplicitPermission_ReturnsNull()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();

            // Act
            LibraryPermissionType? permission = await _permissionRepository.GetUserPermissionAsync(userId, libraryId);

            // Assert
            Assert.IsNull(permission);
        }

        #endregion

        #region GrantPermissionAsync Tests

        [TestMethod]
        public async Task GrantPermissionAsync_WithValidData_CreatesPermission()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();

            // Act
            LibraryPermission createdPermission = await _permissionRepository.GrantPermissionAsync(
                libraryId, userId, LibraryPermissionType.Read, ownerId);

            // Assert
            Assert.IsNotNull(createdPermission);
            Assert.AreNotEqual(Guid.Empty, createdPermission.Id);
            Assert.AreEqual(libraryId, createdPermission.LibraryId);
            Assert.AreEqual(userId, createdPermission.UserId);
            Assert.AreEqual(LibraryPermissionType.Read, createdPermission.PermissionType);
            Assert.AreEqual(ownerId, createdPermission.GrantedByUserId);
            Assert.IsTrue(createdPermission.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [TestMethod]
        public async Task GrantPermissionAsync_WithAllPermissionTypes_CreatesCorrectPermissions()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            LibraryPermissionType[] permissionTypes = { LibraryPermissionType.Read, LibraryPermissionType.Write, LibraryPermissionType.Admin };

            foreach (LibraryPermissionType permissionType in permissionTypes)
            {
                // Create new user for each permission type to avoid duplicates
                Guid testUserId = await CreateTestUser($"user{permissionType}@example.com");

                // Act
                LibraryPermission createdPermission = await _permissionRepository.GrantPermissionAsync(
                    libraryId, testUserId, permissionType, ownerId);

                // Assert
                Assert.AreEqual(permissionType, createdPermission.PermissionType,
                    $"Permission type should be {permissionType}");
            }
        }

        [TestMethod]
        public async Task GrantPermissionAsync_WithDuplicatePermission_ThrowsInvalidOperationException()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Write, ownerId));

            Assert.IsTrue(exception.Message.Contains("already exists"));
        }

        [TestMethod]
        public async Task GrantPermissionAsync_WithNonExistentLibrary_ThrowsInvalidOperationException()
        {
            // Arrange
            Guid ownerId = await CreateTestUser();
            Guid userId = await CreateTestUser("user@example.com");
            Guid nonExistentLibraryId = Guid.NewGuid();

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _permissionRepository.GrantPermissionAsync(nonExistentLibraryId, userId, LibraryPermissionType.Read, ownerId));

            Assert.IsTrue(exception.Message.Contains("does not exist"));
        }

        #endregion

        #region UpdatePermissionAsync Tests

        [TestMethod]
        public async Task UpdatePermissionAsync_WithExistingPermission_UpdatesSuccessfully()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);

            // Act
            LibraryPermission? updatedPermission = await _permissionRepository.UpdatePermissionAsync(
                libraryId, userId, LibraryPermissionType.Admin, ownerId);

            // Assert
            Assert.IsNotNull(updatedPermission);
            Assert.AreEqual(LibraryPermissionType.Admin, updatedPermission.PermissionType);
            Assert.AreEqual(ownerId, updatedPermission.GrantedByUserId);
        }

        [TestMethod]
        public async Task UpdatePermissionAsync_WithNonExistentPermission_ReturnsNull()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();

            // Act
            LibraryPermission? updatedPermission = await _permissionRepository.UpdatePermissionAsync(
                libraryId, userId, LibraryPermissionType.Admin, ownerId);

            // Assert
            Assert.IsNull(updatedPermission);
        }

        #endregion

        #region RevokePermissionAsync Tests

        [TestMethod]
        public async Task RevokePermissionAsync_WithExistingPermission_RevokesSuccessfully()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);

            // Act
            bool result = await _permissionRepository.RevokePermissionAsync(libraryId, userId);

            // Assert
            Assert.IsTrue(result);

            // Verify permission is revoked
            LibraryPermissionType? permission = await _permissionRepository.GetUserPermissionAsync(userId, libraryId);
            Assert.IsNull(permission);
        }

        [TestMethod]
        public async Task RevokePermissionAsync_WithNonExistentPermission_ReturnsFalse()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();

            // Act
            bool result = await _permissionRepository.RevokePermissionAsync(libraryId, userId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region GetAccessibleKnowledgeBaseIdsAsync Tests

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithOwnerAndPermissions_ReturnsAllAccessible()
        {
            // Arrange
            Guid userId = await CreateTestUser();
            Guid otherUserId = await CreateTestUser("other@example.com");

            // Create owned library
            Library ownedLibrary = await _libraryRepository.CreateAsync("Owned Library", "Description", userId, false);

            // Create public library owned by someone else
            Library publicLibrary = await _libraryRepository.CreateAsync("Public Library", "Description", otherUserId, true);

            // Create private library with explicit permission
            Library privateLibrary = await _libraryRepository.CreateAsync("Private Library", "Description", otherUserId, false);
            await _permissionRepository.GrantPermissionAsync(privateLibrary.Id, userId, LibraryPermissionType.Write, otherUserId);

            // Act
            List<Guid> accessibleIds = await _permissionRepository.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Read);

            // Assert
            Assert.AreEqual(3, accessibleIds.Count);
            Assert.IsTrue(accessibleIds.Contains(ownedLibrary.Id));
            Assert.IsTrue(accessibleIds.Contains(publicLibrary.Id));
            Assert.IsTrue(accessibleIds.Contains(privateLibrary.Id));
        }

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithWritePermissionRequired_FiltersCorrectly()
        {
            // Arrange
            Guid userId = await CreateTestUser();
            Guid otherUserId = await CreateTestUser("other@example.com");

            // Create owned library (should be included - owner has all permissions)
            Library ownedLibrary = await _libraryRepository.CreateAsync("Owned Library", "Description", userId, false);

            // Create public library (should NOT be included - public only grants read)
            Library publicLibrary = await _libraryRepository.CreateAsync("Public Library", "Description", otherUserId, true);

            // Create library with read permission (should NOT be included)
            Library readOnlyLibrary = await _libraryRepository.CreateAsync("Read Only Library", "Description", otherUserId, false);
            await _permissionRepository.GrantPermissionAsync(readOnlyLibrary.Id, userId, LibraryPermissionType.Read, otherUserId);

            // Create library with write permission (should be included)
            Library writeLibrary = await _libraryRepository.CreateAsync("Write Library", "Description", otherUserId, false);
            await _permissionRepository.GrantPermissionAsync(writeLibrary.Id, userId, LibraryPermissionType.Write, otherUserId);

            // Act
            List<Guid> accessibleIds = await _permissionRepository.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Write);

            // Assert
            Assert.AreEqual(2, accessibleIds.Count);
            Assert.IsTrue(accessibleIds.Contains(ownedLibrary.Id));
            Assert.IsTrue(accessibleIds.Contains(writeLibrary.Id));
            Assert.IsFalse(accessibleIds.Contains(publicLibrary.Id));
            Assert.IsFalse(accessibleIds.Contains(readOnlyLibrary.Id));
        }

        [TestMethod]
        public async Task GetAccessibleKnowledgeBaseIdsAsync_WithNoAccess_ReturnsEmptyList()
        {
            // Arrange
            Guid userId = await CreateTestUser();
            Guid otherUserId = await CreateTestUser("other@example.com");

            // Create private library owned by someone else with no permissions
            await _libraryRepository.CreateAsync("Inaccessible Library", "Description", otherUserId, false);

            // Act
            List<Guid> accessibleIds = await _permissionRepository.GetAccessibleLibraryIdsAsync(userId);

            // Assert
            Assert.IsNotNull(accessibleIds);
            Assert.AreEqual(0, accessibleIds.Count);
        }

        #endregion

        #region GetPermissionsByKnowledgeBaseAsync Tests

        [TestMethod]
        public async Task GetPermissionsByKnowledgeBaseAsync_WithPermissions_ReturnsAllPermissions()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            Guid user2Id = await CreateTestUser("user2@example.com");

            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);
            await _permissionRepository.GrantPermissionAsync(libraryId, user2Id, LibraryPermissionType.Write, ownerId);

            // Act
            List<LibraryPermission> permissions = await _permissionRepository.GetPermissionsByLibraryAsync(libraryId);

            // Assert
            Assert.AreEqual(2, permissions.Count);
            Assert.IsTrue(permissions.Any(p => p.UserId == userId && p.PermissionType == LibraryPermissionType.Read));
            Assert.IsTrue(permissions.Any(p => p.UserId == user2Id && p.PermissionType == LibraryPermissionType.Write));
            // Verify ordering (newest first)
            Assert.IsTrue(permissions[0].CreatedAt >= permissions[1].CreatedAt);
        }

        [TestMethod]
        public async Task GetPermissionsByKnowledgeBaseAsync_WithNoPermissions_ReturnsEmptyList()
        {
            // Arrange
            (Guid libraryId, _, _) = await CreateTestLibraryAndUsers();

            // Act
            List<LibraryPermission> permissions = await _permissionRepository.GetPermissionsByLibraryAsync(libraryId);

            // Assert
            Assert.IsNotNull(permissions);
            Assert.AreEqual(0, permissions.Count);
        }

        #endregion

        #region GetPermissionsByUserAsync Tests

        [TestMethod]
        public async Task GetPermissionsByUserAsync_WithPermissions_ReturnsAllUserPermissions()
        {
            // Arrange
            Guid ownerId = await CreateTestUser("owner@example.com");
            Guid userId = await CreateTestUser();

            Library library1 = await _libraryRepository.CreateAsync("Library 1", "Description", ownerId, false);
            Library library2 = await _libraryRepository.CreateAsync("Library 2", "Description", ownerId, false);

            await _permissionRepository.GrantPermissionAsync(library1.Id, userId, LibraryPermissionType.Read, ownerId);
            await _permissionRepository.GrantPermissionAsync(library2.Id, userId, LibraryPermissionType.Write, ownerId);

            // Act
            List<LibraryPermission> permissions = await _permissionRepository.GetPermissionsByUserAsync(userId);

            // Assert
            Assert.AreEqual(2, permissions.Count);
            Assert.IsTrue(permissions.Any(p => p.LibraryId == library1.Id && p.PermissionType == LibraryPermissionType.Read));
            Assert.IsTrue(permissions.Any(p => p.LibraryId == library2.Id && p.PermissionType == LibraryPermissionType.Write));
        }

        [TestMethod]
        public async Task GetPermissionsByUserAsync_WithNoPermissions_ReturnsEmptyList()
        {
            // Arrange
            Guid userId = await CreateTestUser();

            // Act
            List<LibraryPermission> permissions = await _permissionRepository.GetPermissionsByUserAsync(userId);

            // Assert
            Assert.IsNotNull(permissions);
            Assert.AreEqual(0, permissions.Count);
        }

        #endregion

        #region RevokeAllPermissionsForKnowledgeBaseAsync Tests

        [TestMethod]
        public async Task RevokeAllPermissionsForKnowledgeBaseAsync_WithPermissions_RevokesAllPermissions()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();
            Guid user2Id = await CreateTestUser("user2@example.com");

            await _permissionRepository.GrantPermissionAsync(libraryId, userId, LibraryPermissionType.Read, ownerId);
            await _permissionRepository.GrantPermissionAsync(libraryId, user2Id, LibraryPermissionType.Write, ownerId);

            // Act
            int revokedCount = await _permissionRepository.RevokeAllPermissionsForLibraryAsync(libraryId);

            // Assert
            Assert.AreEqual(2, revokedCount);

            // Verify all permissions are revoked
            List<LibraryPermission> remainingPermissions = await _permissionRepository.GetPermissionsByLibraryAsync(libraryId);
            Assert.AreEqual(0, remainingPermissions.Count);
        }

        [TestMethod]
        public async Task RevokeAllPermissionsForKnowledgeBaseAsync_WithNoPermissions_ReturnsZero()
        {
            // Arrange
            (Guid libraryId, _, _) = await CreateTestLibraryAndUsers();

            // Act
            int revokedCount = await _permissionRepository.RevokeAllPermissionsForLibraryAsync(libraryId);

            // Assert
            Assert.AreEqual(0, revokedCount);
        }

        #endregion

        #region RevokeAllPermissionsForUserAsync Tests

        [TestMethod]
        public async Task RevokeAllPermissionsForUserAsync_WithPermissions_RevokesAllUserPermissions()
        {
            // Arrange
            Guid ownerId = await CreateTestUser("owner@example.com");
            Guid userId = await CreateTestUser();

            Library library1 = await _libraryRepository.CreateAsync("Library 1", "Description", ownerId, false);
            Library library2 = await _libraryRepository.CreateAsync("Library 2", "Description", ownerId, false);

            await _permissionRepository.GrantPermissionAsync(library1.Id, userId, LibraryPermissionType.Read, ownerId);
            await _permissionRepository.GrantPermissionAsync(library2.Id, userId, LibraryPermissionType.Write, ownerId);

            // Act
            int revokedCount = await _permissionRepository.RevokeAllPermissionsForUserAsync(userId);

            // Assert
            Assert.AreEqual(2, revokedCount);

            // Verify all user permissions are revoked
            List<LibraryPermission> remainingPermissions = await _permissionRepository.GetPermissionsByUserAsync(userId);
            Assert.AreEqual(0, remainingPermissions.Count);
        }

        [TestMethod]
        public async Task RevokeAllPermissionsForUserAsync_WithNoPermissions_ReturnsZero()
        {
            // Arrange
            Guid userId = await CreateTestUser();

            // Act
            int revokedCount = await _permissionRepository.RevokeAllPermissionsForUserAsync(userId);

            // Assert
            Assert.AreEqual(0, revokedCount);
        }

        #endregion

        #region Integration and Edge Case Tests

        [TestMethod]
        public async Task PermissionHierarchy_WorksCorrectly()
        {
            // Arrange
            (Guid libraryId, Guid ownerId, Guid userId) = await CreateTestLibraryAndUsers();

            // Test each permission level
            LibraryPermissionType[] permissionLevels = { LibraryPermissionType.Read, LibraryPermissionType.Write, LibraryPermissionType.Admin };

            foreach (LibraryPermissionType grantedPermission in permissionLevels)
            {
                Guid testUserId = await CreateTestUser($"test{grantedPermission}@example.com");
                await _permissionRepository.GrantPermissionAsync(libraryId, testUserId, grantedPermission, ownerId);

                // Act & Assert - Check permission hierarchy
                foreach (LibraryPermissionType requiredPermission in permissionLevels)
                {
                    bool hasPermission = await _permissionRepository.HasPermissionAsync(testUserId, libraryId, requiredPermission);
                    bool shouldHavePermission = grantedPermission >= requiredPermission;

                    Assert.AreEqual(shouldHavePermission, hasPermission,
                        $"User with {grantedPermission} permission should {(shouldHavePermission ? "" : "NOT ")}have {requiredPermission} access");
                }
            }
        }

        [TestMethod]
        public async Task ComplexPermissionScenario_WorksCorrectly()
        {
            // Arrange
            Guid owner1Id = await CreateTestUser("owner1@example.com");
            Guid owner2Id = await CreateTestUser("owner2@example.com");
            Guid userId = await CreateTestUser();

            // Create different types of libraries
            Library ownedLibrary = await _libraryRepository.CreateAsync("Owned Library", null, userId, false);
            Library publicLibrary = await _libraryRepository.CreateAsync("Public Library", null, owner1Id, true);
            Library privateWithReadLibrary = await _libraryRepository.CreateAsync("Private Read Library", null, owner1Id, false);
            Library privateWithWriteLibrary = await _libraryRepository.CreateAsync("Private Write Library", null, owner2Id, false);
            Library inaccessibleLibrary = await _libraryRepository.CreateAsync("Inaccessible Library", null, owner2Id, false);

            // Grant specific permissions
            await _permissionRepository.GrantPermissionAsync(privateWithReadLibrary.Id, userId, LibraryPermissionType.Read, owner1Id);
            await _permissionRepository.GrantPermissionAsync(privateWithWriteLibrary.Id, userId, LibraryPermissionType.Write, owner2Id);

            // Act & Assert - Test various access scenarios
            // Owned library - should have all permissions
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, ownedLibrary.Id, LibraryPermissionType.Admin));

            // Public library - should only have read
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, publicLibrary.Id, LibraryPermissionType.Read));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, publicLibrary.Id, LibraryPermissionType.Write));

            // Private library with read permission
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, privateWithReadLibrary.Id, LibraryPermissionType.Read));
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, privateWithReadLibrary.Id, LibraryPermissionType.Write));

            // Private library with write permission - should have both read and write
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, privateWithWriteLibrary.Id, LibraryPermissionType.Read));
            Assert.IsTrue(await _permissionRepository.HasPermissionAsync(userId, privateWithWriteLibrary.Id, LibraryPermissionType.Write));

            // Inaccessible library
            Assert.IsFalse(await _permissionRepository.HasPermissionAsync(userId, inaccessibleLibrary.Id, LibraryPermissionType.Read));

            // Test GetAccessibleKnowledgeBaseIdsAsync
            List<Guid> readAccessibleIds = await _permissionRepository.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Read);
            Assert.AreEqual(4, readAccessibleIds.Count); // Owned + Public + Private Read + Private Write

            List<Guid> writeAccessibleIds = await _permissionRepository.GetAccessibleLibraryIdsAsync(userId, LibraryPermissionType.Write);
            Assert.AreEqual(2, writeAccessibleIds.Count); // Owned + Private Write
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test user and returns the ID.
        /// </summary>
        private async Task<Guid> CreateTestUser(string email = "test@example.com")
        {
            User user = await _userRepository.CreateAsync(
                email, "hashed_password", "John", "Doe", new List<string> { "user" });
            return user.Id;
        }

        /// <summary>
        /// Creates a test library with an owner and a regular user.
        /// </summary>
        private async Task<(Guid LibraryId, Guid OwnerId, Guid UserId)> CreateTestLibraryAndUsers(bool isPublic = false)
        {
            Guid ownerId = await CreateTestUser("owner@example.com");
            Guid userId = await CreateTestUser();

            Library library = await _libraryRepository.CreateAsync("Test Library", "A test library", ownerId, isPublic);

            return (library.Id, ownerId, userId);
        }

        #endregion
    }
}

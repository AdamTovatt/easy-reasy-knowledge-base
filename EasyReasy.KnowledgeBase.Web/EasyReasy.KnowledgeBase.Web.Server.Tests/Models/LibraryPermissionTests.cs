using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Models
{
    /// <summary>
    /// Unit tests for the LibraryPermission model.
    /// </summary>
    [TestClass]
    public class LibraryPermissionTests : GeneralTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            LoadTestEnvironmentVariables();
        }

        #region Constructor Tests - Valid Data

        [TestMethod]
        public void Constructor_WithValidData_CreatesInstance()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Guid libraryId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            LibraryPermissionType permissionType = LibraryPermissionType.Write;
            Guid grantedByUserId = Guid.NewGuid();
            DateTime createdAt = DateTime.UtcNow.AddDays(-2);

            // Act
            LibraryPermission permission = new LibraryPermission(
                id, libraryId, userId, permissionType, grantedByUserId, createdAt);

            // Assert
            Assert.AreEqual(id, permission.Id);
            Assert.AreEqual(libraryId, permission.LibraryId);
            Assert.AreEqual(userId, permission.UserId);
            Assert.AreEqual(permissionType, permission.PermissionType);
            Assert.AreEqual(grantedByUserId, permission.GrantedByUserId);
            Assert.AreEqual(createdAt, permission.CreatedAt);
        }

        [TestMethod]
        public void Constructor_WithReadPermission_CreatesInstance()
        {
            // Arrange
            LibraryPermissionType permissionType = LibraryPermissionType.Read;

            // Act
            LibraryPermission permission = CreateTestPermission(permissionType: permissionType);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Read, permission.PermissionType);
        }

        [TestMethod]
        public void Constructor_WithWritePermission_CreatesInstance()
        {
            // Arrange
            LibraryPermissionType permissionType = LibraryPermissionType.Write;

            // Act
            LibraryPermission permission = CreateTestPermission(permissionType: permissionType);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Write, permission.PermissionType);
        }

        [TestMethod]
        public void Constructor_WithAdminPermission_CreatesInstance()
        {
            // Arrange
            LibraryPermissionType permissionType = LibraryPermissionType.Admin;

            // Act
            LibraryPermission permission = CreateTestPermission(permissionType: permissionType);

            // Assert
            Assert.AreEqual(LibraryPermissionType.Admin, permission.PermissionType);
        }

        [TestMethod]
        public void Constructor_WithAllPermissionTypes_CreatesInstances()
        {
            // Arrange
            LibraryPermissionType[] permissionTypes =
            {
                LibraryPermissionType.Read,
                LibraryPermissionType.Write,
                LibraryPermissionType.Admin
            };

            // Act & Assert
            foreach (LibraryPermissionType permissionType in permissionTypes)
            {
                LibraryPermission permission = CreateTestPermission(permissionType: permissionType);

                Assert.AreEqual(permissionType, permission.PermissionType,
                    $"Permission type should be {permissionType}");
            }
        }

        #endregion

        #region Constructor Tests - Edge Cases

        [TestMethod]
        public void Constructor_WithEmptyGuids_CreatesInstance()
        {
            // Arrange
            Guid emptyId = Guid.Empty;
            Guid emptyLibraryId = Guid.Empty;
            Guid emptyUserId = Guid.Empty;
            Guid emptyGrantedByUserId = Guid.Empty;

            // Act
            LibraryPermission permission = CreateTestPermission(
                id: emptyId,
                libraryId: emptyLibraryId,
                userId: emptyUserId,
                grantedByUserId: emptyGrantedByUserId);

            // Assert
            Assert.AreEqual(emptyId, permission.Id);
            Assert.AreEqual(emptyLibraryId, permission.LibraryId);
            Assert.AreEqual(emptyUserId, permission.UserId);
            Assert.AreEqual(emptyGrantedByUserId, permission.GrantedByUserId);
        }

        [TestMethod]
        public void Constructor_WithMinDateTime_CreatesInstance()
        {
            // Arrange
            DateTime minDate = DateTime.MinValue;

            // Act
            LibraryPermission permission = CreateTestPermission(createdAt: minDate);

            // Assert
            Assert.AreEqual(minDate, permission.CreatedAt);
        }

        [TestMethod]
        public void Constructor_WithMaxDateTime_CreatesInstance()
        {
            // Arrange
            DateTime maxDate = DateTime.MaxValue;

            // Act
            LibraryPermission permission = CreateTestPermission(createdAt: maxDate);

            // Assert
            Assert.AreEqual(maxDate, permission.CreatedAt);
        }

        [TestMethod]
        public void Constructor_WithSameUserAndGrantedBy_CreatesInstance()
        {
            // Arrange - User granting permission to themselves (edge case)
            Guid sameUserId = Guid.NewGuid();

            // Act
            LibraryPermission permission = CreateTestPermission(
                userId: sameUserId,
                grantedByUserId: sameUserId);

            // Assert
            Assert.AreEqual(sameUserId, permission.UserId);
            Assert.AreEqual(sameUserId, permission.GrantedByUserId);
        }

        #endregion

        #region Permission Type Enum Tests

        [TestMethod]
        public void PermissionType_ReadValue_EqualsZero()
        {
            // Act & Assert - Verify enum values (useful for database storage)
            Assert.AreEqual(0, (int)LibraryPermissionType.Read);
        }

        [TestMethod]
        public void PermissionType_WriteValue_EqualsOne()
        {
            // Act & Assert
            Assert.AreEqual(1, (int)LibraryPermissionType.Write);
        }

        [TestMethod]
        public void PermissionType_AdminValue_EqualsTwo()
        {
            // Act & Assert
            Assert.AreEqual(2, (int)LibraryPermissionType.Admin);
        }

        [TestMethod]
        public void PermissionType_AllEnumValues_AreValid()
        {
            // Arrange
            LibraryPermissionType[] expectedValues =
            {
                LibraryPermissionType.Read,
                LibraryPermissionType.Write,
                LibraryPermissionType.Admin
            };

            // Act
            LibraryPermissionType[] actualValues = (LibraryPermissionType[])Enum.GetValues(typeof(LibraryPermissionType));

            // Assert
            Assert.AreEqual(expectedValues.Length, actualValues.Length);
            CollectionAssert.AreEquivalent(expectedValues, actualValues);
        }

        [TestMethod]
        public void PermissionType_ToStringValues_AreCorrect()
        {
            // Act & Assert
            Assert.AreEqual("Read", LibraryPermissionType.Read.ToString());
            Assert.AreEqual("Write", LibraryPermissionType.Write.ToString());
            Assert.AreEqual("Admin", LibraryPermissionType.Admin.ToString());
        }

        [TestMethod]
        public void PermissionType_IsDefined_ForAllValues()
        {
            // Act & Assert - Verify all enum values are defined
            Assert.IsTrue(Enum.IsDefined(typeof(LibraryPermissionType), LibraryPermissionType.Read));
            Assert.IsTrue(Enum.IsDefined(typeof(LibraryPermissionType), LibraryPermissionType.Write));
            Assert.IsTrue(Enum.IsDefined(typeof(LibraryPermissionType), LibraryPermissionType.Admin));
        }

        [TestMethod]
        public void PermissionType_InvalidValue_IsNotDefined()
        {
            // Act & Assert - Test invalid enum values
            Assert.IsFalse(Enum.IsDefined(typeof(LibraryPermissionType), (LibraryPermissionType)99));
            Assert.IsFalse(Enum.IsDefined(typeof(LibraryPermissionType), (LibraryPermissionType)(-1)));
        }

        #endregion

        #region Property Immutability Tests

        [TestMethod]
        public void Properties_AreReadOnly()
        {
            // Arrange
            LibraryPermission permission = CreateTestPermission();

            // Act & Assert - Verify all properties are read-only (get-only)
            System.Reflection.PropertyInfo? idProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.Id));
            System.Reflection.PropertyInfo? libraryIdProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.LibraryId));
            System.Reflection.PropertyInfo? userIdProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.UserId));
            System.Reflection.PropertyInfo? permissionTypeProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.PermissionType));
            System.Reflection.PropertyInfo? grantedByUserIdProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.GrantedByUserId));
            System.Reflection.PropertyInfo? createdAtProperty = typeof(LibraryPermission).GetProperty(nameof(LibraryPermission.CreatedAt));

            Assert.IsNotNull(idProperty);
            Assert.IsNull(idProperty.SetMethod, "Id should be read-only");

            Assert.IsNotNull(libraryIdProperty);
            Assert.IsNull(libraryIdProperty.SetMethod, "LibraryId should be read-only");

            Assert.IsNotNull(userIdProperty);
            Assert.IsNull(userIdProperty.SetMethod, "UserId should be read-only");

            Assert.IsNotNull(permissionTypeProperty);
            Assert.IsNull(permissionTypeProperty.SetMethod, "PermissionType should be read-only");

            Assert.IsNotNull(grantedByUserIdProperty);
            Assert.IsNull(grantedByUserIdProperty.SetMethod, "GrantedByUserId should be read-only");

            Assert.IsNotNull(createdAtProperty);
            Assert.IsNull(createdAtProperty.SetMethod, "CreatedAt should be read-only");
        }

        #endregion

        #region Date and Time Tests

        [TestMethod]
        public void Constructor_WithSpecificDateTime_StoresExactValue()
        {
            // Arrange
            DateTime specificDate = new DateTime(2024, 1, 15, 14, 30, 45, 123, DateTimeKind.Utc);

            // Act
            LibraryPermission permission = CreateTestPermission(createdAt: specificDate);

            // Assert
            Assert.AreEqual(specificDate, permission.CreatedAt);
            Assert.AreEqual(123, permission.CreatedAt.Millisecond);
            Assert.AreEqual(DateTimeKind.Utc, permission.CreatedAt.Kind);
        }

        [TestMethod]
        public void Constructor_WithLocalDateTime_PreservesKind()
        {
            // Arrange
            DateTime localDate = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Local);

            // Act
            LibraryPermission permission = CreateTestPermission(createdAt: localDate);

            // Assert
            Assert.AreEqual(localDate, permission.CreatedAt);
            Assert.AreEqual(DateTimeKind.Local, permission.CreatedAt.Kind);
        }

        #endregion

        #region Integration Scenarios

        [TestMethod]
        public void Constructor_OwnerGrantingReadPermission_CreatesCorrectly()
        {
            // Arrange - Simulate library owner granting read permission to another user
            Guid libraryId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();    // Library owner
            Guid readerId = Guid.NewGuid();   // User receiving permission
            LibraryPermissionType permissionType = LibraryPermissionType.Read;

            // Act
            LibraryPermission permission = CreateTestPermission(
                libraryId: libraryId,
                userId: readerId,
                permissionType: permissionType,
                grantedByUserId: ownerId);

            // Assert
            Assert.AreEqual(libraryId, permission.LibraryId);
            Assert.AreEqual(readerId, permission.UserId);
            Assert.AreEqual(LibraryPermissionType.Read, permission.PermissionType);
            Assert.AreEqual(ownerId, permission.GrantedByUserId);
            Assert.AreNotEqual(permission.UserId, permission.GrantedByUserId);
        }

        [TestMethod]
        public void Constructor_AdminGrantingWritePermission_CreatesCorrectly()
        {
            // Arrange - Simulate admin granting write permission
            Guid adminId = Guid.NewGuid();
            Guid writerId = Guid.NewGuid();
            LibraryPermissionType permissionType = LibraryPermissionType.Write;

            // Act
            LibraryPermission permission = CreateTestPermission(
                userId: writerId,
                permissionType: permissionType,
                grantedByUserId: adminId);

            // Assert
            Assert.AreEqual(writerId, permission.UserId);
            Assert.AreEqual(LibraryPermissionType.Write, permission.PermissionType);
            Assert.AreEqual(adminId, permission.GrantedByUserId);
        }

        [TestMethod]
        public void Constructor_UpgradingPermission_CreatesCorrectly()
        {
            // Arrange - Simulate upgrading a user's permission from Read to Admin
            Guid libraryId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid adminId = Guid.NewGuid();

            // Create initial Read permission
            LibraryPermission readPermission = CreateTestPermission(
                libraryId: libraryId,
                userId: userId,
                permissionType: LibraryPermissionType.Read,
                grantedByUserId: adminId,
                createdAt: DateTime.UtcNow.AddDays(-7));

            // Act - Create new Admin permission (would replace the Read permission in practice)
            LibraryPermission adminPermission = CreateTestPermission(
                libraryId: libraryId,
                userId: userId,
                permissionType: LibraryPermissionType.Admin,
                grantedByUserId: adminId,
                createdAt: DateTime.UtcNow);

            // Assert
            Assert.AreEqual(libraryId, readPermission.LibraryId);
            Assert.AreEqual(libraryId, adminPermission.LibraryId);
            Assert.AreEqual(userId, readPermission.UserId);
            Assert.AreEqual(userId, adminPermission.UserId);
            Assert.AreEqual(LibraryPermissionType.Read, readPermission.PermissionType);
            Assert.AreEqual(LibraryPermissionType.Admin, adminPermission.PermissionType);
            Assert.IsTrue(adminPermission.CreatedAt > readPermission.CreatedAt);
        }

        #endregion

        #region Multiple Permissions Scenarios

        [TestMethod]
        public void Constructor_MultiplePermissionsForSameLibrary_CreatesDistinctInstances()
        {
            // Arrange - Multiple users getting different permissions on same library
            Guid libraryId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();
            Guid user1Id = Guid.NewGuid();
            Guid user2Id = Guid.NewGuid();
            Guid user3Id = Guid.NewGuid();

            // Act
            LibraryPermission readPermission = CreateTestPermission(
                libraryId: libraryId,
                userId: user1Id,
                permissionType: LibraryPermissionType.Read,
                grantedByUserId: ownerId);

            LibraryPermission writePermission = CreateTestPermission(
                libraryId: libraryId,
                userId: user2Id,
                permissionType: LibraryPermissionType.Write,
                grantedByUserId: ownerId);

            LibraryPermission adminPermission = CreateTestPermission(
                libraryId: libraryId,
                userId: user3Id,
                permissionType: LibraryPermissionType.Admin,
                grantedByUserId: ownerId);

            // Assert
            Assert.AreEqual(libraryId, readPermission.LibraryId);
            Assert.AreEqual(libraryId, writePermission.LibraryId);
            Assert.AreEqual(libraryId, adminPermission.LibraryId);

            Assert.AreNotEqual(readPermission.UserId, writePermission.UserId);
            Assert.AreNotEqual(writePermission.UserId, adminPermission.UserId);
            Assert.AreNotEqual(readPermission.UserId, adminPermission.UserId);

            Assert.AreEqual(LibraryPermissionType.Read, readPermission.PermissionType);
            Assert.AreEqual(LibraryPermissionType.Write, writePermission.PermissionType);
            Assert.AreEqual(LibraryPermissionType.Admin, adminPermission.PermissionType);

            Assert.AreNotEqual(readPermission.Id, writePermission.Id);
            Assert.AreNotEqual(writePermission.Id, adminPermission.Id);
            Assert.AreNotEqual(readPermission.Id, adminPermission.Id);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test LibraryPermission with default values, allowing specific properties to be overridden.
        /// </summary>
        private LibraryPermission CreateTestPermission(
            Guid? id = null,
            Guid? libraryId = null,
            Guid? userId = null,
            LibraryPermissionType permissionType = LibraryPermissionType.Read,
            Guid? grantedByUserId = null,
            DateTime? createdAt = null)
        {
            return new LibraryPermission(
                id ?? Guid.NewGuid(),
                libraryId ?? Guid.NewGuid(),
                userId ?? Guid.NewGuid(),
                permissionType,
                grantedByUserId ?? Guid.NewGuid(),
                createdAt ?? DateTime.UtcNow.AddMinutes(-30)
            );
        }

        #endregion
    }
}

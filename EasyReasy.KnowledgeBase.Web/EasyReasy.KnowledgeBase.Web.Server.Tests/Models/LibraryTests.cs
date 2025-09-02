using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Tests.TestUtilities.BaseClasses;

namespace EasyReasy.KnowledgeBase.Web.Server.Tests.Models
{
    /// <summary>
    /// Unit tests for the Library model.
    /// </summary>
    [TestClass]
    public class LibraryTests : GeneralTestBase
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
            string name = "Test Library";
            string description = "A test library for unit testing";
            Guid ownerId = Guid.NewGuid();
            bool isPublic = false;
            DateTime createdAt = DateTime.UtcNow.AddDays(-5);
            DateTime updatedAt = DateTime.UtcNow.AddDays(-1);

            // Act
            Library library = new Library(id, name, description, ownerId, isPublic, createdAt, updatedAt);

            // Assert
            Assert.AreEqual(id, library.Id);
            Assert.AreEqual(name, library.Name);
            Assert.AreEqual(description, library.Description);
            Assert.AreEqual(ownerId, library.OwnerId);
            Assert.AreEqual(isPublic, library.IsPublic);
            Assert.AreEqual(createdAt, library.CreatedAt);
            Assert.AreEqual(updatedAt, library.UpdatedAt);
        }

        [TestMethod]
        public void Constructor_WithNullDescription_CreatesInstance()
        {
            // Arrange
            string name = "Library with Null Description";
            string? description = null;

            // Act
            Library library = CreateTestLibrary(name: name, description: description);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.IsNull(library.Description);
        }

        [TestMethod]
        public void Constructor_WithEmptyDescription_CreatesInstance()
        {
            // Arrange
            string name = "Library with Empty Description";
            string description = "";

            // Act
            Library library = CreateTestLibrary(name: name, description: description);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.AreEqual("", library.Description);
        }

        [TestMethod]
        public void Constructor_WithPublicLibrary_CreatesInstance()
        {
            // Arrange
            string name = "Public Library";
            bool isPublic = true;

            // Act
            Library library = CreateTestLibrary(name: name, isPublic: isPublic);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.IsTrue(library.IsPublic);
        }

        [TestMethod]
        public void Constructor_WithPrivateLibrary_CreatesInstance()
        {
            // Arrange
            string name = "Private Library";
            bool isPublic = false;

            // Act
            Library library = CreateTestLibrary(name: name, isPublic: isPublic);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.IsFalse(library.IsPublic);
        }

        #endregion

        #region Constructor Tests - Null Name Validation

        [TestMethod]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            string? nullName = null;

            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                CreateTestLibrary(name: nullName!));

            Assert.AreEqual("name", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_WithNullNameDirectCall_ThrowsArgumentNullException()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            string? nullName = null;
            string description = "Test Description";
            Guid ownerId = Guid.NewGuid();
            bool isPublic = false;
            DateTime now = DateTime.UtcNow;

            // Act & Assert
            ArgumentNullException exception = Assert.ThrowsException<ArgumentNullException>(() =>
                new Library(id, nullName!, description, ownerId, isPublic, now, now));

            Assert.AreEqual("name", exception.ParamName);
        }

        #endregion

        #region Constructor Tests - Edge Cases

        [TestMethod]
        public void Constructor_WithEmptyGuids_CreatesInstance()
        {
            // Arrange
            Guid emptyId = Guid.Empty;
            Guid emptyOwnerId = Guid.Empty;

            // Act
            Library library = CreateTestLibrary(id: emptyId, ownerId: emptyOwnerId);

            // Assert
            Assert.AreEqual(emptyId, library.Id);
            Assert.AreEqual(emptyOwnerId, library.OwnerId);
        }

        [TestMethod]
        public void Constructor_WithMinDateTime_CreatesInstance()
        {
            // Arrange
            DateTime minDate = DateTime.MinValue;

            // Act
            Library library = CreateTestLibrary(createdAt: minDate, updatedAt: minDate);

            // Assert
            Assert.AreEqual(minDate, library.CreatedAt);
            Assert.AreEqual(minDate, library.UpdatedAt);
        }

        [TestMethod]
        public void Constructor_WithMaxDateTime_CreatesInstance()
        {
            // Arrange
            DateTime maxDate = DateTime.MaxValue;

            // Act
            Library library = CreateTestLibrary(createdAt: maxDate, updatedAt: maxDate);

            // Assert
            Assert.AreEqual(maxDate, library.CreatedAt);
            Assert.AreEqual(maxDate, library.UpdatedAt);
        }

        [TestMethod]
        public void Constructor_WithUpdatedAtBeforeCreatedAt_CreatesInstance()
        {
            // Arrange - Technically invalid but model doesn't validate this
            DateTime createdAt = DateTime.UtcNow;
            DateTime updatedAt = createdAt.AddDays(-1); // Updated before created

            // Act
            Library library = CreateTestLibrary(createdAt: createdAt, updatedAt: updatedAt);

            // Assert - Model allows this (business logic validation would be elsewhere)
            Assert.AreEqual(createdAt, library.CreatedAt);
            Assert.AreEqual(updatedAt, library.UpdatedAt);
            Assert.IsTrue(library.UpdatedAt < library.CreatedAt);
        }

        #endregion

        #region Name Tests

        [TestMethod]
        public void Constructor_WithWhitespaceOnlyName_CreatesInstance()
        {
            // Arrange
            string whitespaceName = "   ";

            // Act
            Library library = CreateTestLibrary(name: whitespaceName);

            // Assert - Model allows whitespace-only names (business validation would be elsewhere)
            Assert.AreEqual(whitespaceName, library.Name);
        }

        [TestMethod]
        public void Constructor_WithEmptyStringName_CreatesInstance()
        {
            // Arrange
            string emptyName = "";

            // Act
            Library library = CreateTestLibrary(name: emptyName);

            // Assert - Model allows empty names (business validation would be elsewhere)
            Assert.AreEqual(emptyName, library.Name);
        }

        [TestMethod]
        public void Constructor_WithUnicodeName_CreatesInstance()
        {
            // Arrange
            string unicodeName = "测试图书馆 📚 Émojis & Special-Chars_123";

            // Act
            Library library = CreateTestLibrary(name: unicodeName);

            // Assert
            Assert.AreEqual(unicodeName, library.Name);
        }

        [TestMethod]
        public void Constructor_WithVeryLongName_CreatesInstance()
        {
            // Arrange
            string longName = new string('A', 1000);

            // Act
            Library library = CreateTestLibrary(name: longName);

            // Assert
            Assert.AreEqual(longName, library.Name);
            Assert.AreEqual(1000, library.Name.Length);
        }

        [TestMethod]
        public void Constructor_WithSpecialCharactersInName_CreatesInstance()
        {
            // Arrange
            string specialName = "Library with /\\:*?\"<>| special chars & symbols @#$%^&*()";

            // Act
            Library library = CreateTestLibrary(name: specialName);

            // Assert
            Assert.AreEqual(specialName, library.Name);
        }

        #endregion

        #region Description Tests

        [TestMethod]
        public void Constructor_WithUnicodeDescription_CreatesInstance()
        {
            // Arrange
            string unicodeDescription = "This is a library with unicode: 测试 🚀 émojis and special chars";

            // Act
            Library library = CreateTestLibrary(description: unicodeDescription);

            // Assert
            Assert.AreEqual(unicodeDescription, library.Description);
        }

        [TestMethod]
        public void Constructor_WithVeryLongDescription_CreatesInstance()
        {
            // Arrange
            string longDescription = new string('B', 5000);

            // Act
            Library library = CreateTestLibrary(description: longDescription);

            // Assert
            Assert.AreEqual(longDescription, library.Description);
            Assert.AreEqual(5000, library.Description!.Length);
        }

        [TestMethod]
        public void Constructor_WithMultilineDescription_CreatesInstance()
        {
            // Arrange
            string multilineDescription = "Line 1\nLine 2\r\nLine 3\nWith various line endings";

            // Act
            Library library = CreateTestLibrary(description: multilineDescription);

            // Assert
            Assert.AreEqual(multilineDescription, library.Description);
            Assert.IsTrue(library.Description.Contains('\n'));
            Assert.IsTrue(library.Description.Contains('\r'));
        }

        #endregion

        #region Property Immutability Tests

        [TestMethod]
        public void Properties_AreReadOnly()
        {
            // Arrange
            Library library = CreateTestLibrary();

            // Act & Assert - Verify all properties are read-only (get-only)
            System.Reflection.PropertyInfo? idProperty = typeof(Library).GetProperty(nameof(Library.Id));
            System.Reflection.PropertyInfo? nameProperty = typeof(Library).GetProperty(nameof(Library.Name));
            System.Reflection.PropertyInfo? descriptionProperty = typeof(Library).GetProperty(nameof(Library.Description));
            System.Reflection.PropertyInfo? ownerIdProperty = typeof(Library).GetProperty(nameof(Library.OwnerId));
            System.Reflection.PropertyInfo? isPublicProperty = typeof(Library).GetProperty(nameof(Library.IsPublic));
            System.Reflection.PropertyInfo? createdAtProperty = typeof(Library).GetProperty(nameof(Library.CreatedAt));
            System.Reflection.PropertyInfo? updatedAtProperty = typeof(Library).GetProperty(nameof(Library.UpdatedAt));

            Assert.IsNotNull(idProperty);
            Assert.IsNull(idProperty.SetMethod, "Id should be read-only");

            Assert.IsNotNull(nameProperty);
            Assert.IsNull(nameProperty.SetMethod, "Name should be read-only");

            Assert.IsNotNull(descriptionProperty);
            Assert.IsNull(descriptionProperty.SetMethod, "Description should be read-only");

            Assert.IsNotNull(ownerIdProperty);
            Assert.IsNull(ownerIdProperty.SetMethod, "OwnerId should be read-only");

            Assert.IsNotNull(isPublicProperty);
            Assert.IsNull(isPublicProperty.SetMethod, "IsPublic should be read-only");

            Assert.IsNotNull(createdAtProperty);
            Assert.IsNull(createdAtProperty.SetMethod, "CreatedAt should be read-only");

            Assert.IsNotNull(updatedAtProperty);
            Assert.IsNull(updatedAtProperty.SetMethod, "UpdatedAt should be read-only");
        }

        #endregion

        #region Date Comparison Tests

        [TestMethod]
        public void Constructor_WithSpecificDateTimes_StoresExactValues()
        {
            // Arrange
            DateTime createdAt = new DateTime(2024, 1, 15, 10, 30, 45, 123, DateTimeKind.Utc);
            DateTime updatedAt = new DateTime(2024, 1, 20, 14, 45, 30, 456, DateTimeKind.Utc);

            // Act
            Library library = CreateTestLibrary(createdAt: createdAt, updatedAt: updatedAt);

            // Assert
            Assert.AreEqual(createdAt, library.CreatedAt);
            Assert.AreEqual(updatedAt, library.UpdatedAt);
            Assert.AreEqual(123, library.CreatedAt.Millisecond);
            Assert.AreEqual(456, library.UpdatedAt.Millisecond);
            Assert.AreEqual(DateTimeKind.Utc, library.CreatedAt.Kind);
            Assert.AreEqual(DateTimeKind.Utc, library.UpdatedAt.Kind);
        }

        [TestMethod]
        public void Constructor_WithSameDateTimes_AllowsDuplicates()
        {
            // Arrange
            DateTime sameDateTime = DateTime.UtcNow;

            // Act
            Library library = CreateTestLibrary(createdAt: sameDateTime, updatedAt: sameDateTime);

            // Assert
            Assert.AreEqual(sameDateTime, library.CreatedAt);
            Assert.AreEqual(sameDateTime, library.UpdatedAt);
            Assert.AreEqual(library.CreatedAt, library.UpdatedAt);
        }

        #endregion

        #region Integration Scenarios

        [TestMethod]
        public void Constructor_WithTypicalUserScenario_CreatesCorrectly()
        {
            // Arrange - Simulate a typical user creating a library
            string name = "My Document Library";
            string description = "A personal library for storing my important documents and files.";
            bool isPublic = false; // Private by default
            DateTime now = DateTime.UtcNow;

            // Act
            Library library = CreateTestLibrary(
                name: name,
                description: description,
                isPublic: isPublic,
                createdAt: now,
                updatedAt: now);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.AreEqual(description, library.Description);
            Assert.IsFalse(library.IsPublic);
            Assert.AreNotEqual(Guid.Empty, library.Id);
            Assert.AreNotEqual(Guid.Empty, library.OwnerId);
        }

        [TestMethod]
        public void Constructor_WithPublicLibraryScenario_CreatesCorrectly()
        {
            // Arrange - Simulate creating a public library
            string name = "Open Source Documentation";
            string description = "Public library containing open source project documentation.";
            bool isPublic = true;

            // Act
            Library library = CreateTestLibrary(
                name: name,
                description: description,
                isPublic: isPublic);

            // Assert
            Assert.AreEqual(name, library.Name);
            Assert.AreEqual(description, library.Description);
            Assert.IsTrue(library.IsPublic);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test Library with default values, allowing specific properties to be overridden.
        /// </summary>
        private Library CreateTestLibrary(
            Guid? id = null,
            string name = "Test Library",
            string? description = "A test library for unit testing purposes",
            Guid? ownerId = null,
            bool isPublic = false,
            DateTime? createdAt = null,
            DateTime? updatedAt = null)
        {
            DateTime now = DateTime.UtcNow;

            return new Library(
                id ?? Guid.NewGuid(),
                name,
                description,
                ownerId ?? Guid.NewGuid(),
                isPublic,
                createdAt ?? now.AddMinutes(-10),
                updatedAt ?? now.AddMinutes(-5)
            );
        }

        #endregion
    }
}

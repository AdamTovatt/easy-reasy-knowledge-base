using EasyReasy.KnowledgeBase.Web.Server.Models;

namespace EasyReasy.KnowledgeBase.Web.Server.Repositories
{
    /// <summary>
    /// Defines the contract for library data access operations.
    /// </summary>
    public interface ILibraryRepository
    {
        /// <summary>
        /// Creates a new library.
        /// </summary>
        /// <param name="name">The name of the library.</param>
        /// <param name="description">The description of the library.</param>
        /// <param name="ownerId">The unique identifier of the user who will own the library.</param>
        /// <param name="isPublic">Whether the library should be publicly readable.</param>
        /// <returns>The created library.</returns>
        Task<Library> CreateAsync(string name, string? description, Guid ownerId, bool isPublic = false);

        /// <summary>
        /// Gets a library by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the library.</param>
        /// <returns>The library if found, null otherwise.</returns>
        Task<Library?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a library by its name.
        /// </summary>
        /// <param name="name">The name of the library.</param>
        /// <returns>The library if found, null otherwise.</returns>
        Task<Library?> GetByNameAsync(string name);

        /// <summary>
        /// Updates an existing library.
        /// </summary>
        /// <param name="knowledgeBase">The library to update.</param>
        /// <returns>The updated library.</returns>
        Task<Library> UpdateAsync(Library knowledgeBase);

        /// <summary>
        /// Deletes a library by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the library to delete.</param>
        /// <returns>True if the library was deleted, false if it didn't exist.</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Gets all librarys owned by a specific user.
        /// </summary>
        /// <param name="ownerId">The unique identifier of the owner.</param>
        /// <returns>A list of librarys owned by the user.</returns>
        Task<List<Library>> GetByOwnerIdAsync(Guid ownerId);

        /// <summary>
        /// Gets all public librarys.
        /// </summary>
        /// <returns>A list of all public librarys.</returns>
        Task<List<Library>> GetPublicLibrariesAsync();

        /// <summary>
        /// Checks if a library exists.
        /// </summary>
        /// <param name="id">The unique identifier of the library.</param>
        /// <returns>True if the library exists, false otherwise.</returns>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Checks if a user is the owner of a library.
        /// </summary>
        /// <param name="libraryId">The unique identifier of the library.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>True if the user is the owner, false otherwise.</returns>
        Task<bool> IsOwnerAsync(Guid libraryId, Guid userId);
    }
}

using Quraaa.Application.Features.Admin.Common;
using Quraaa.Domain.Author;
using Quraaa.Domain.Library;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Interfaces
{
    /// <summary>
    /// Administrator-side reads and lifecycle actions over the platform's core
    /// records. Deactivation is the existing soft delete, so a deactivated
    /// record disappears from every ordinary query but stays recoverable;
    /// permanent removal is separate and only ever allowed once a record is
    /// deactivated AND nothing references it.
    /// </summary>
    public interface IAdminModerationRepository
    {
        Task<(IReadOnlyCollection<AdminUserResponse> Items, int TotalCount)> GetUsersAsync(
            string? searchTerm,
            Role? role,
            bool includeDeactivated,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<AdminAuthorResponse> Items, int TotalCount)> GetAuthorsAsync(
            string? searchTerm,
            bool includeDeactivated,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<AdminLibraryResponse> Items, int TotalCount)> GetLibrariesAsync(
            string? searchTerm,
            bool includeDeactivated,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>What still references each record, keyed by record id.</summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetUserDeletionBlockersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetAuthorDeletionBlockersAsync(
            IReadOnlyCollection<Guid> authorIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetLibraryDeletionBlockersAsync(
            IReadOnlyCollection<Guid> libraryIds,
            CancellationToken cancellationToken = default);

        Task<int> CountSuperAdminsAsync(CancellationToken cancellationToken = default);

        /// <summary>Loads records regardless of their deactivated state.</summary>
        Task<IReadOnlyCollection<UserAggregate>> GetUsersByIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<AuthorAggregate>> GetAuthorsByIdsAsync(
            IReadOnlyCollection<Guid> authorIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<LibraryAggregate>> GetLibrariesByIdsAsync(
            IReadOnlyCollection<Guid> libraryIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently removes records and, for users, their sign-in identity.
        /// Callers must already have verified the records are deactivated and
        /// unreferenced.
        /// </summary>
        Task RemoveUsersAsync(
            IReadOnlyCollection<UserAggregate> users,
            CancellationToken cancellationToken = default);

        void RemoveAuthors(IReadOnlyCollection<AuthorAggregate> authors);

        void RemoveLibraries(IReadOnlyCollection<LibraryAggregate> libraries);

        /// <summary>
        /// Creates a new super admin: the sign-in identity, both identity roles
        /// (Admin and SuperAdmin), and the platform profile.
        /// </summary>
        Task<AdminUserResponse> CreateSuperAdminAsync(
            string phoneNumber,
            string password,
            string firstName,
            string lastName,
            Guid createdBy,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

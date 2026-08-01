using Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRepository
    {
        Task<bool> ExistsByUserIdAsync(Guid userId);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsApprovedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<LibraryAggregate?> GetApprovedByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddLibraryAsync(LibraryAggregate library);
        Task<(IReadOnlyCollection<LibraryAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<ListingSummaryResponse> Items, int TotalCount)> GetLibraryBooksAsync(
            Guid libraryId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default);
        Task<bool> ExistsByIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default);

        // Needed by AddPhysicalBook handler to resolve the caller's library
        Task<LibraryAggregate?> GetApprovedByUserIdAsync(Guid userId,
            CancellationToken cancellationToken = default);

        Task<LibraryAggregate?> GetByIdAsync(Guid id,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<LibraryRequestResponse> Items, int TotalCount)> GetRequestsAsync(
            LibraryApprovalStatus? status,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync();
    }
}

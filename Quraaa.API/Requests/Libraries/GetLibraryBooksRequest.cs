using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.API.Requests.Libraries
{
    public record GetLibraryBooksRequest(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string? SortBy = null,
        ListingStatus? Status = null,
        bool SortDescending = false
    );
}

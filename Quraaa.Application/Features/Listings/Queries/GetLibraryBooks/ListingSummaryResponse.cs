using Quraaa.Application.Features.Listings.Common;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Listings.Queries.GetLibraryBooks
{
    // Status omitted deliberately — this query only ever returns Active
    // listings (see the repository's Where clause), so it'd be a constant.
    public record ListingSummaryResponse(
        Guid ListingId,
        decimal Price,
        int? Stock,
        BookCondition? Condition,
        BookDetails Book
    );
}
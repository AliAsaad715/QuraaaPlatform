using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Listings.Queries.GetListingById
{
    public record ListingDetailsResponse(
        Guid ListingId,
        decimal Price,
        int? Stock,
        BookCondition? Condition,
        ListingStatus Status,
        ListingFormat Format,
        int Version,
        BookDetails Book
    );
}
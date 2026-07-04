using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Listings.Queries.GetListingById
{
    public record GetListingByIdQuery(Guid ListingId)
        : IRequest<AppResult<ListingDetailsResponse>>;
}
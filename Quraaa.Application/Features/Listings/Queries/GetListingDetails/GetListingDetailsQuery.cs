using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Listings.Queries.GetListingDetails
{
    public record GetListingDetailsQuery(Guid Id) : IRequest<AppResult<ListingDetailsResponse>>;
}

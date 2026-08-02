using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.ReactivateListing
{
    public record ReactivateListingCommand(
        [property: JsonIgnore] Guid RequestingUserId,
        Guid ListingId) : IRequest<AppResult>;
}
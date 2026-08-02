using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.RemoveListing
{
    public record RemoveListingCommand(
        [property: JsonIgnore] Guid RequestingUserId,
        Guid ListingId) : IRequest<AppResult>;
}
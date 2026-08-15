using MediatR;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListingDigitalAsset
{
    public record UpdateListingDigitalAssetCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        [JsonIgnore]
        public Guid ListingId { get; init; }

        public IUploadedFile DigitalAsset { get; init; } = null!;
    }
}

using MediatR;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.AddDigitalBook
{
    public record AddDigitalBookCommand : IRequest<AppResult<AddDigitalBookResponse>>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public decimal Price { get; init; }
        public string Isbn { get; init; } = null!;
        public IUploadedFile DigitalAsset { get; init; } = null!;
    }
}
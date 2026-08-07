using MediatR;
using Quraaa.Application.Features.Purchases.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Purchases.Commands.PurchaseDigitalBook
{
    public sealed record PurchaseDigitalBookCommand : IRequest<AppResult<PurchaseDigitalBookResponse>>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }
        public Guid ListingId { get; init; }
    }
}

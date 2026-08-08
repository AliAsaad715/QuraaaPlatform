using MediatR;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Purchases.Queries.GetPurchaseDigitalAsset
{
    /// <summary>
    /// Resolves the digital file backing a purchase, authorized to <see cref="RequestingUserId"/>.
    /// The returned descriptor carries the caching metadata (ETag/LastModifiedUtc/ContentLength)
    /// the streaming controller needs to answer conditional and range requests.
    /// </summary>
    public sealed record GetPurchaseDigitalAssetQuery(Guid PurchaseId, Guid RequestingUserId)
        : IRequest<AppResult<DigitalAssetFileDescriptor>>;
}

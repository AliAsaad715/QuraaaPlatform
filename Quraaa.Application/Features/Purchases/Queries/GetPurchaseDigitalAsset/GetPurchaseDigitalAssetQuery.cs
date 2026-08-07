using MediatR;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Purchases.Queries.GetPurchaseDigitalAsset
{
    public sealed record GetPurchaseDigitalAssetQuery(Guid PurchaseId, Guid RequestingUserId)
        : IRequest<AppResult<DigitalAssetFileDescriptor>>;
}

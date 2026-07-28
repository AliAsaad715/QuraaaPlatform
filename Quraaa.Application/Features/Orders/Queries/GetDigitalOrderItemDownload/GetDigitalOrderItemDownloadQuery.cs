using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Queries.GetDigitalOrderItemDownload
{
    public record GetDigitalOrderItemDownloadQuery(
        Guid BuyerUserId,
        Guid OrderId,
        Guid OrderItemId)
        : IRequest<AppResult<DigitalOrderItemDownloadResponse>>;
}

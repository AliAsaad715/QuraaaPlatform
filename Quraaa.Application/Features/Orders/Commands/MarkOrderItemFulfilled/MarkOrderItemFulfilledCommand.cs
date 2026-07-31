using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemFulfilled
{
    public sealed record MarkOrderItemFulfilledCommand(
        Guid RequestingUserId,
        Guid OrderId,
        Guid OrderItemId)
        : IRequest<AppResult<SellerOrderItemFulfillmentResponse>>;
}

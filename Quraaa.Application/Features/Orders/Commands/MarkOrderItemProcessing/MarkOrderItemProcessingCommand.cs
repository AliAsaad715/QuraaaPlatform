using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemProcessing
{
    public sealed record MarkOrderItemProcessingCommand(
        Guid RequestingUserId,
        Guid OrderId,
        Guid OrderItemId)
        : IRequest<AppResult<SellerOrderItemFulfillmentResponse>>;
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Orders;
using Quraaa.Application.Features.Orders.Commands.MarkOrderItemFulfilled;
using Quraaa.Application.Features.Orders.Commands.MarkOrderItemProcessing;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Queries.GetSellerOrderItems;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = nameof(Role.User) + "," + nameof(Role.LibraryOwner))]
    [ApiController]
    [Route("api/seller/orders")]
    public sealed class SellerOrdersController : ApiClientController
    {
        [HttpGet]
        [ProducesResponseType(
            typeof(PagedResult<SellerOrderItemResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrderItems(
            [FromQuery] GetSellerOrderItemsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetSellerOrderItemsQuery(
                    userId,
                    request.PageNumber,
                    request.PageSize,
                    request.FulfillmentStatus),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("{orderId:guid}/items/{orderItemId:guid}/processing")]
        [ProducesResponseType(
            typeof(SellerOrderItemFulfillmentResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MarkItemProcessing(
            [FromRoute] Guid orderId,
            [FromRoute] Guid orderItemId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new MarkOrderItemProcessingCommand(
                    userId,
                    orderId,
                    orderItemId),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("{orderId:guid}/items/{orderItemId:guid}/fulfilled")]
        [ProducesResponseType(
            typeof(SellerOrderItemFulfillmentResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MarkItemFulfilled(
            [FromRoute] Guid orderId,
            [FromRoute] Guid orderItemId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new MarkOrderItemFulfilledCommand(
                    userId,
                    orderId,
                    orderItemId),
                cancellationToken);

            return HandleResult(result);
        }
    }
}

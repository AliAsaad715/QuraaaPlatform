using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Quraaa.API.Requests.Orders;
using Quraaa.Application.Features.Orders.Commands.ArchiveOrder;
using Quraaa.Application.Features.Orders.Commands.CancelOrder;
using Quraaa.Application.Features.Orders.Commands.CreateOrder;
using Quraaa.Application.Features.Orders.Commands.CreateOrderCheckoutSession;
using Quraaa.Application.Features.Orders.Commands.UpdateOrderShippingLocation;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Queries.GetMyOrders;
using Quraaa.Application.Features.Orders.Queries.GetOrderCheckoutContext;
using Quraaa.Application.Features.Orders.Queries.GetOrderById;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = nameof(Role.User))]
    [Route("api/orders")]
    public class OrdersController : ApiClientController
    {
        [HttpGet("checkout-context")]
        [ProducesResponseType(typeof(OrderCheckoutContextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetCheckoutContext(
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetOrderCheckoutContextQuery(userId),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrderCheckoutResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new CreateOrderCommand(
                userId,
                request.SuccessUrl,
                request.CancelUrl,
                request.ShippingLocation?.Latitude,
                request.ShippingLocation?.Longitude,
                request.ShippingLocationId), cancellationToken);

            return HandleResult(
                result,
                response => CreatedAtAction(
                    nameof(GetOrderById),
                    new { orderId = response.Order.OrderId },
                    response));
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(PagedResult<OrderSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] GetMyOrdersRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new GetMyOrdersQuery(
                userId,
                request.PageNumber,
                request.PageSize,
                request.Status), cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderById(
            [FromRoute] Guid orderId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetOrderByIdQuery(userId, orderId),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPut("{orderId:guid}/shipping-location")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateShippingLocation(
            [FromRoute] Guid orderId,
            [FromBody] UpdateOrderShippingLocationRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new UpdateOrderShippingLocationCommand(
                userId,
                orderId,
                request.Latitude,
                request.Longitude,
                request.ShippingLocationId), cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("{orderId:guid}/checkout-session")]
        [ProducesResponseType(typeof(OrderCheckoutResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateCheckoutSession(
            [FromRoute] Guid orderId,
            [FromBody] CreateOrderCheckoutSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new CreateOrderCheckoutSessionCommand(
                userId,
                orderId,
                request.SuccessUrl,
                request.CancelUrl), cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("{orderId:guid}/cancel")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelOrder(
            [FromRoute] Guid orderId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
            CancelOrderRequest? request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new CancelOrderCommand(userId, orderId, request?.Reason),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete("{orderId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ArchiveOrder(
            [FromRoute] Guid orderId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new ArchiveOrderCommand(userId, orderId),
                cancellationToken);

            return HandleResult(result);
        }
    }
}

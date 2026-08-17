using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Quraaa.API.Requests.Orders;
using Quraaa.Application.Features.Orders.Commands.ArchiveOrder;
using Quraaa.Application.Features.Orders.Commands.CancelOrder;
using Quraaa.Application.Features.Orders.Commands.CreateOrder;
using Quraaa.Application.Features.Orders.Commands.ConfirmCheckoutSession;
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
                ResolveReturnUrl(request.SuccessUrl, succeeded: true, orderId: null),
                ResolveReturnUrl(request.CancelUrl, succeeded: false, orderId: null),
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
                ResolveReturnUrl(request.SuccessUrl, succeeded: true, orderId),
                ResolveReturnUrl(request.CancelUrl, succeeded: false, orderId)), cancellationToken);

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

        // ── POST /api/orders/checkout/confirm ───────────────────────────────
        /// <summary>
        /// Settles a checkout after the buyer returns from the payment page, and
        /// reports the result. Call this instead of trusting the redirect: the
        /// redirect only means the buyer came back, whereas this verifies the
        /// payment with the provider and marks the order paid if it went
        /// through — so it works even when the provider webhook is delayed or
        /// cannot reach this server.
        ///
        /// Safe to call repeatedly. <c>pending: true</c> means the payment is
        /// not settled yet, so poll for a few seconds before showing a failure.
        /// </summary>
        /// <param name="request">The session id returned when checkout started.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The checkout status after confirmation.</response>
        /// <response code="404">No checkout of yours matches this session.</response>
        [HttpPost("checkout/confirm")]
        [ProducesResponseType(typeof(CheckoutStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmCheckout(
            [FromBody] ConfirmCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new ConfirmCheckoutSessionCommand(userId, request.SessionId ?? string.Empty),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Falls back to this API's app return page when the caller does not
        /// supply its own URL. Stripe only accepts http/https here, so a mobile
        /// client cannot pass its deep link directly — the return page performs
        /// the hand-off instead.
        /// </summary>
        private string ResolveReturnUrl(string? providedUrl, bool succeeded, Guid? orderId)
        {
            if (!string.IsNullOrWhiteSpace(providedUrl))
            {
                return providedUrl.Trim();
            }

            var options = HttpContext.RequestServices
                .GetRequiredService<CheckoutRedirectOptions>();

            var baseUrl = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}"
                : options.PublicBaseUrl.TrimEnd('/');

            var query = succeeded ? "status=success" : "status=cancel";

            if (orderId.HasValue && orderId.Value != Guid.Empty)
            {
                query += $"&orderId={orderId.Value:D}";
            }

            if (succeeded)
            {
                // Stripe substitutes this placeholder for the real session id,
                // but ONLY when the success URL asks for it. Without it the app
                // gets no session to correlate the return with.
                query += "&session_id={CHECKOUT_SESSION_ID}";
            }

            return $"{baseUrl.TrimEnd('/')}/checkout/return?{query}";
        }

    }
}

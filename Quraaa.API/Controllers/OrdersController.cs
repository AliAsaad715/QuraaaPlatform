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
using Quraaa.Application.Features.Orders.Queries.GetDigitalOrderItemDownload;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OrdersController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

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

        [HttpGet("{orderId:guid}/items/{orderItemId:guid}/download")]
        [Produces("application/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDigitalItemDownload(
            [FromRoute] Guid orderId,
            [FromRoute] Guid orderItemId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetDigitalOrderItemDownloadQuery(userId, orderId, orderItemId),
                cancellationToken);

            return HandleResult(result, CreateDigitalFileResult);
        }

        private IActionResult CreateDigitalFileResult(DigitalOrderItemDownloadResponse download)
        {
            var normalizedAssetPath = download.DigitalAssetPath
                .Replace('\\', '/')
                .TrimStart('/');

            // Existing snapshots used the former public URL. Resolve that
            // logical prefix into the private store without serving wwwroot.
            if (normalizedAssetPath.StartsWith(
                    "uploads/books/",
                    StringComparison.OrdinalIgnoreCase))
            {
                normalizedAssetPath =
                    $"books/{normalizedAssetPath["uploads/books/".Length..]}";
            }

            if (!normalizedAssetPath.StartsWith("books/", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    Path.GetExtension(normalizedAssetPath),
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            string physicalAssetPath;
            string booksRootPrefix;

            try
            {
                var booksRootPath = Path.GetFullPath(
                    Path.Combine(
                        _webHostEnvironment.ContentRootPath,
                        "storage",
                        "books"));
                physicalAssetPath = Path.GetFullPath(
                    Path.Combine(
                        _webHostEnvironment.ContentRootPath,
                        "storage",
                        normalizedAssetPath.Replace('/', Path.DirectorySeparatorChar)));
                booksRootPrefix = booksRootPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                    or NotSupportedException
                    or PathTooLongException)
            {
                return NotFound();
            }

            var pathComparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!physicalAssetPath.StartsWith(booksRootPrefix, pathComparison)
                || !System.IO.File.Exists(physicalAssetPath))
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(
                physicalAssetPath,
                "application/pdf",
                Path.GetFileName(physicalAssetPath),
                enableRangeProcessing: true);
        }
    }
}

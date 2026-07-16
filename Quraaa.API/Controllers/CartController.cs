using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Carts.Commands.AddCartItem;
using Quraaa.Application.Features.Carts.Commands.ClearCart;
using Quraaa.Application.Features.Carts.Commands.CreateCheckoutSession;
using Quraaa.Application.Features.Carts.Commands.RemoveCartItem;
using Quraaa.Application.Features.Carts.Commands.UpdateCartItemQuantity;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Queries.GetMyCart;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : ApiClientController
    {
        [HttpGet("me")]
        [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCart()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new GetMyCartQuery(userId));
            return HandleResult(result);
        }

        [HttpPost("items")]
        [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemCommand command)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId });
            return HandleResult(result);
        }

        [HttpPut("items/{listingId:guid}")]
        [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateItemQuantity([FromRoute] Guid listingId, [FromBody] UpdateCartItemQuantityCommand command)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId, ListingId = listingId });
            return HandleResult(result);
        }

        [HttpDelete("items/{listingId:guid}")]
        [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem([FromRoute] Guid listingId)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new RemoveCartItemCommand(userId, listingId));
            return HandleResult(result);
        }

        [HttpDelete("me")]
        [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearCart()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new ClearCartCommand(userId));
            return HandleResult(result);
        }

        [HttpPost("checkout")]
        [ProducesResponseType(typeof(StripeCheckoutSessionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Checkout([FromBody] CreateCheckoutSessionCommand command)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId });
            return HandleResult(result);
        }
    }
}

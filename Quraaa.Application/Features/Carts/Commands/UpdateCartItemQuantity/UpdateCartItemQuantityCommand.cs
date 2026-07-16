using MediatR;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Carts.Commands.UpdateCartItemQuantity
{
    public record UpdateCartItemQuantityCommand([property: JsonIgnore] Guid UserId, [property: JsonIgnore] Guid ListingId, int Quantity) : IRequest<AppResult<CartResponse>>;
}

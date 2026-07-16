using MediatR;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Carts.Commands.AddCartItem
{
    public record AddCartItemCommand([property: JsonIgnore] Guid UserId, Guid ListingId, int Quantity) : IRequest<AppResult<CartResponse>>;
}

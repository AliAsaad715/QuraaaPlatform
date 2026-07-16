using MediatR;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Carts.Commands.RemoveCartItem
{
    public record RemoveCartItemCommand([property: JsonIgnore] Guid UserId, Guid ListingId) : IRequest<AppResult<CartResponse>>;
}

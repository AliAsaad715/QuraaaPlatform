using MediatR;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Carts.Queries.GetMyCart
{
    public record GetMyCartQuery([property: JsonIgnore] Guid UserId) : IRequest<AppResult<CartResponse>>;
}

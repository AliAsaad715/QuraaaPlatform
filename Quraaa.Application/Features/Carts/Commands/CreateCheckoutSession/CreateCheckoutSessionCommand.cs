using MediatR;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Carts.Commands.CreateCheckoutSession
{
    public record CreateCheckoutSessionCommand(
        [property: JsonIgnore] Guid UserId,
        string SuccessUrl,
        string CancelUrl) : IRequest<AppResult<StripeCheckoutSessionResponse>>;
}

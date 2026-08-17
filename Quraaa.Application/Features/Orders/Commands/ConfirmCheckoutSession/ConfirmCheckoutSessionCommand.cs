using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Orders.Commands.ConfirmCheckoutSession
{
    /// <summary>
    /// Settles what became of a checkout session and reports it.
    ///
    /// The provider webhook is still the normal confirmation path, but it can be
    /// late, blocked by a firewall, or unconfigured in development. This asks the
    /// provider directly and finalizes the order if the money is in — so a buyer
    /// coming back from payment is never left staring at an unpaid order.
    /// Idempotent: calling it on an already-paid order just reports the state.
    /// </summary>
    public record ConfirmCheckoutSessionCommand(
        [property: JsonIgnore] Guid BuyerUserId,
        string SessionId) : IRequest<AppResult<CheckoutStatusResponse>>;
}

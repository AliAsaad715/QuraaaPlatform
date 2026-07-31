using MediatR;

namespace Quraaa.Application.Features.Orders.Commands.ReconcileExpiredOrderPayment
{
    public sealed record ReconcileExpiredOrderPaymentCommand(
        Guid OrderId,
        DateTime ExpiredOnOrBeforeUtc)
        : IRequest<bool>;
}

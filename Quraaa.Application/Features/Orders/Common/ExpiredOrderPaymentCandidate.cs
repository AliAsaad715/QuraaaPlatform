namespace Quraaa.Application.Features.Orders.Common
{
    public sealed record ExpiredOrderPaymentCandidate(
        Guid OrderId,
        DateTime ExpiresAtUtc,
        string OrderNumber);
}

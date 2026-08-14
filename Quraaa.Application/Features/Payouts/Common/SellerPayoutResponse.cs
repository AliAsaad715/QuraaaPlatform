using Quraaa.Domain.Payouts.Enums;

namespace Quraaa.Application.Features.Payouts.Common
{
    public record SellerPayoutResponse(
        Guid PayoutId,
        Guid OrderId,
        string OrderNumber,
        string Currency,
        decimal GrossAmount,
        decimal CommissionPercent,
        decimal PlatformFee,
        decimal NetAmount,
        SellerPayoutStatus Status,
        int AttemptCount,
        DateTime? PaidAtUtc,
        string? StripeTransferId,
        string? FailureReason,
        DateTime CreationTime);
}

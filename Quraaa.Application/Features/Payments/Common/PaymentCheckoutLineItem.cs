namespace Quraaa.Application.Features.Payments.Common
{
    /// <summary>
    /// A checkout line item whose amount has already been calculated and trusted by the application.
    /// </summary>
    public sealed record PaymentCheckoutLineItem(
        string Name,
        string? Description,
        long UnitAmountMinor,
        long Quantity);
}

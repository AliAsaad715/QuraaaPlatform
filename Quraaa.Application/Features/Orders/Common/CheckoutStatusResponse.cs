namespace Quraaa.Application.Features.Orders.Common
{
    /// <summary>
    /// Whether a checkout session actually resulted in a paid order.
    ///
    /// This is the only trustworthy answer about a payment: the browser redirect
    /// can be replayed or forged, whereas this reflects the provider webhook the
    /// server verified.
    /// </summary>
    /// <param name="Paid">True once the payment is confirmed.</param>
    /// <param name="Pending">
    /// True when the session is known but the confirmation has not arrived yet —
    /// the client should poll briefly rather than report a failure.
    /// </param>
    public record CheckoutStatusResponse(
        bool Paid,
        bool Pending,
        Guid? OrderId,
        string? OrderNumber,
        string OrderStatus,
        string PaymentStatus);
}

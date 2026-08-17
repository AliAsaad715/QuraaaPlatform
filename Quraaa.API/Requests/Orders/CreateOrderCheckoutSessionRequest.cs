namespace Quraaa.API.Requests.Orders
{
    /// <param name="SuccessUrl">
    /// Optional. Omit it and checkout returns to this API's app return page,
    /// which hands off to the mobile app.
    /// </param>
    /// <param name="CancelUrl">Optional; same default as SuccessUrl.</param>
    public record CreateOrderCheckoutSessionRequest(
        string? SuccessUrl = null,
        string? CancelUrl = null);
}

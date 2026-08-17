namespace Quraaa.API.Requests.Orders
{
    /// <param name="SessionId">
    /// The checkout session id returned when the session was created (the app
    /// already holds it, so the payment page does not need to echo it back).
    /// </param>
    public record ConfirmCheckoutRequest(string? SessionId);
}

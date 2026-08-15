namespace Quraaa.API.Requests.Libraries
{
    /// <summary>
    /// Registration wizard, Stripe step. The token is sent in the JSON body so
    /// it is not written to API query logs.
    /// </summary>
    public sealed class LibraryRegistrationStripeOnboardingRequest
    {
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Optional: where Stripe sends the owner after onboarding. Must be on
        /// an allow-listed frontend origin; defaults to the dashboard register
        /// URL with <c>?stripe=return</c>.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Optional: where Stripe sends the owner if the onboarding link
        /// expired. Defaults to the dashboard register URL with
        /// <c>?stripe=refresh</c>.
        /// </summary>
        public string? RefreshUrl { get; set; }
    }
}

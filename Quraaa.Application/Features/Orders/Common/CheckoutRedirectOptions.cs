namespace Quraaa.Application.Features.Orders.Common
{
    /// <summary>
    /// Where Stripe sends a buyer when hosted checkout finishes.
    ///
    /// Stripe only accepts http/https return URLs, so a mobile app cannot be
    /// sent a custom-scheme deep link directly. Checkout therefore returns to a
    /// small page on this API, which immediately hands off to the app.
    /// </summary>
    public sealed class CheckoutRedirectOptions
    {
        /// <summary>
        /// The app's URL scheme, without "://" — e.g. <c>quraaa</c>. Leave empty
        /// to disable the hand-off and show only the web fallback.
        /// </summary>
        public string MobileAppScheme { get; set; } = string.Empty;

        /// <summary>Deep-link path used after a completed payment.</summary>
        public string MobileSuccessPath { get; set; } = "checkout/success";

        /// <summary>Deep-link path used when the buyer backs out.</summary>
        public string MobileCancelPath { get; set; } = "checkout/cancel";

        /// <summary>
        /// Absolute base URL of this API as reachable from the buyer's browser.
        /// Leave empty to derive it from the incoming request, which is correct
        /// behind a properly configured reverse proxy.
        /// </summary>
        public string PublicBaseUrl { get; set; } = string.Empty;

        public bool HasMobileHandoff => !string.IsNullOrWhiteSpace(MobileAppScheme);

        public string BuildDeepLink(bool succeeded, Guid? orderId, string? sessionId)
        {
            var path = (succeeded ? MobileSuccessPath : MobileCancelPath)
                .Trim()
                .TrimStart('/');

            var query = new List<string>();

            if (orderId.HasValue && orderId.Value != Guid.Empty)
            {
                query.Add($"orderId={Uri.EscapeDataString(orderId.Value.ToString("D"))}");
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                query.Add($"sessionId={Uri.EscapeDataString(sessionId)}");
            }

            var suffix = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;

            return $"{MobileAppScheme.Trim()}://{path}{suffix}";
        }
    }
}

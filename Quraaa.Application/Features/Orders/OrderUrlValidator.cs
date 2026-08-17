namespace Quraaa.Application.Features.Orders
{
    internal static class OrderUrlValidator
    {
        public const string InvalidRedirectUrlMessage =
            "Checkout return URLs must be absolute HTTP or HTTPS URLs. The payment " +
            "provider rejects custom app schemes, so omit these fields to use the " +
            "app return page instead.";

        public static bool IsAllowedRedirectUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }
    }
}

namespace Quraaa.Application.Features.Orders
{
    internal static class OrderUrlValidator
    {
        public static bool IsAllowedRedirectUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }
    }
}

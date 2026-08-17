using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Orders.Common;
using System.Net;
using System.Text;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Where Stripe sends the buyer when hosted checkout ends.
    ///
    /// Stripe only accepts http/https return URLs, so the app cannot be opened
    /// directly from checkout. This page is the bridge: it hands off to the
    /// mobile app over its own scheme and, if the app is not installed or the
    /// buyer paid on desktop, stays as a readable confirmation page.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("checkout")]
    public sealed class CheckoutReturnController : ControllerBase
    {
        private readonly CheckoutRedirectOptions _options;

        public CheckoutReturnController(CheckoutRedirectOptions options)
        {
            _options = options;
        }

        // ── GET /checkout/return ─────────────────────────────────────────────
        /// <summary>
        /// The page Stripe redirects to. Not an API call — it returns HTML for a
        /// browser.
        /// </summary>
        /// <param name="status">"success" when the payment completed.</param>
        /// <param name="orderId">The order, when the caller knew it up front.</param>
        /// <param name="sessionId">Stripe's checkout session id, if present.</param>
        [HttpGet("return")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Produces("text/html")]
        public ContentResult Return(
            [FromQuery] string? status = null,
            [FromQuery] Guid? orderId = null,
            [FromQuery(Name = "session_id")] string? sessionId = null)
        {
            var succeeded = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

            // The payment is confirmed by the Stripe webhook, never by this
            // redirect: a buyer can reach this URL without paying. The wording
            // and the app both treat it as "returned", not "paid".
            var deepLink = _options.HasMobileHandoff
                ? _options.BuildDeepLink(succeeded, orderId, sessionId)
                : null;

            Response.Headers.CacheControl = "no-store";

            return new ContentResult
            {
                ContentType = "text/html; charset=utf-8",
                StatusCode = StatusCodes.Status200OK,
                Content = BuildPage(succeeded, deepLink),
            };
        }

        private static string BuildPage(bool succeeded, string? deepLink)
        {
            var titleAr = succeeded ? "تم الدفع بنجاح" : "تم إلغاء الدفع";
            var titleEn = succeeded ? "Payment complete" : "Payment cancelled";
            var bodyAr = succeeded
                ? "شكراً لك. جارٍ إعادتك إلى التطبيق لعرض طلبك."
                : "لم يكتمل الدفع. يمكنك المحاولة مرة أخرى من التطبيق.";
            var bodyEn = succeeded
                ? "Thanks. Returning you to the app to view your order."
                : "The payment was not completed. You can try again from the app.";

            var accent = succeeded ? "#1a7f4b" : "#a4552b";
            var glyph = succeeded ? "&#10003;" : "&#10005;";

            var handoff = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(deepLink))
            {
                var encodedLink = WebUtility.HtmlEncode(deepLink);

                handoff.Append(
                    $"<a class=\"button\" href=\"{encodedLink}\">فتح التطبيق &middot; Open the app</a>");

                // location.replace keeps this page out of the back stack, so
                // returning from the app does not land on it again.
                handoff.Append(
                    "<script>setTimeout(function(){location.replace(" +
                    JavaScriptString(deepLink) +
                    ");},400);</script>");
            }

            // $$ raw string: interpolation is {{expr}}, so the CSS braces below
            // stay literal without escaping.
            return $$"""
                <!doctype html>
                <html lang="ar" dir="rtl">
                <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <meta name="robots" content="noindex">
                <title>{{WebUtility.HtmlEncode(titleAr)}}</title>
                <style>
                  :root { color-scheme: light dark; }
                  body { margin:0; min-height:100vh; display:flex; align-items:center;
                         justify-content:center; background:#f6f8f6; color:#12261c;
                         font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif; }
                  .card { background:#fff; border-radius:16px; padding:32px 28px; max-width:26rem;
                          width:calc(100% - 2rem); box-shadow:0 10px 40px rgba(0,0,0,.08);
                          text-align:center; }
                  .mark { width:64px; height:64px; border-radius:50%; margin:0 auto 20px;
                          display:flex; align-items:center; justify-content:center;
                          font-size:32px; color:#fff; background:{{accent}}; }
                  h1 { font-size:1.35rem; margin:0 0 8px; }
                  p { margin:0 0 6px; line-height:1.6; }
                  .en { color:#5c6b62; font-size:.9rem; direction:ltr; }
                  .button { display:inline-block; margin-top:22px; padding:12px 22px;
                            border-radius:10px; background:{{accent}}; color:#fff;
                            text-decoration:none; font-weight:600; }
                  @media (prefers-color-scheme: dark) {
                    body { background:#0f1512; color:#eaf2ed; }
                    .card { background:#18211c; box-shadow:none; }
                    .en { color:#9fb1a6; }
                  }
                </style>
                </head>
                <body>
                  <main class="card">
                    <div class="mark">{{glyph}}</div>
                    <h1>{{WebUtility.HtmlEncode(titleAr)}}</h1>
                    <p>{{WebUtility.HtmlEncode(bodyAr)}}</p>
                    <p class="en"><strong>{{WebUtility.HtmlEncode(titleEn)}}</strong><br>{{WebUtility.HtmlEncode(bodyEn)}}</p>
                    {{handoff}}
                  </main>
                </body>
                </html>
                """;
        }

        /// <summary>
        /// Emits a JavaScript string literal. The value is built from configured
        /// paths and request ids, but it is escaped anyway so nothing can break
        /// out of the literal.
        /// </summary>
        private static string JavaScriptString(string value)
        {
            var builder = new StringBuilder("\"");

            foreach (var character in value)
            {
                builder.Append(character switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '<' => "\\u003c",
                    '>' => "\\u003e",
                    '&' => "\\u0026",
                    '\'' => "\\u0027",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    _ => character.ToString(),
                });
            }

            return builder.Append('"').ToString();
        }
    }
}

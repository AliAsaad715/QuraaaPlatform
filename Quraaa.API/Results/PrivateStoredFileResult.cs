using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Quraaa.Application.Shared.Files;
using HttpHeaders = System.Net.Http.Headers.HttpHeaders;

namespace Quraaa.API.Results
{
    /// <summary>
    /// Delivers an already-authorized private asset. Legacy local files use MVC's
    /// PhysicalFileResult; Cloudinary files are proxied from a short-lived signed URL
    /// so provider credentials/references never become the public API contract.
    /// </summary>
    public sealed class PrivateStoredFileResult : IActionResult
    {
        private static readonly string[] ForwardedRequestHeaders =
        [
            HeaderNames.Range,
            HeaderNames.IfRange,
            HeaderNames.IfNoneMatch,
            HeaderNames.IfModifiedSince
        ];

        private readonly DigitalAssetFileDescriptor _descriptor;
        private readonly bool _asAttachment;
        private readonly string _cacheControl;

        public PrivateStoredFileResult(
            DigitalAssetFileDescriptor descriptor,
            bool asAttachment,
            string cacheControl)
        {
            _descriptor = descriptor;
            _asAttachment = asAttachment;
            _cacheControl = cacheControl;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (_descriptor.PhysicalPath is not null)
            {
                await ExecutePhysicalFileAsync(context);
                return;
            }

            if (_descriptor.RemoteDownloadUri is not null)
            {
                await ExecuteRemoteFileAsync(context);
                return;
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        }

        private Task ExecutePhysicalFileAsync(ActionContext context)
        {
            var physicalPath = _descriptor.PhysicalPath;
            if (string.IsNullOrWhiteSpace(physicalPath) || !File.Exists(physicalPath))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            SetPrivateResponseHeaders(context.HttpContext.Response);

            var result = new PhysicalFileResult(physicalPath, _descriptor.ContentType)
            {
                EnableRangeProcessing = true,
                LastModified = _descriptor.LastModifiedUtc,
                EntityTag = string.IsNullOrWhiteSpace(_descriptor.ETag)
                    ? null
                    : new EntityTagHeaderValue(_descriptor.ETag)
            };

            if (_asAttachment)
            {
                result.FileDownloadName = _descriptor.DownloadFileName;
            }
            else
            {
                SetContentDisposition(context.HttpContext.Response, "inline");
            }

            return result.ExecuteResultAsync(context);
        }

        private async Task ExecuteRemoteFileAsync(ActionContext context)
        {
            var httpContext = context.HttpContext;
            var response = httpContext.Response;
            var clientFactory = httpContext.RequestServices
                .GetRequiredService<IHttpClientFactory>();
            var logger = httpContext.RequestServices
                .GetRequiredService<ILogger<PrivateStoredFileResult>>();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                _descriptor.RemoteDownloadUri);

            foreach (var headerName in ForwardedRequestHeaders)
            {
                if (httpContext.Request.Headers.TryGetValue(headerName, out var values))
                {
                    request.Headers.TryAddWithoutValidation(
                        headerName,
                        values.ToArray());
                }
            }

            HttpResponseMessage upstream;
            try
            {
                upstream = await clientFactory
                    .CreateClient("PrivateAssetDelivery")
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        httpContext.RequestAborted);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "Private Cloudinary file delivery failed before a response was received ({ExceptionType}).",
                    exception.GetType().Name);
                response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            using (upstream)
            {
                if (upstream.StatusCode is System.Net.HttpStatusCode.NotFound
                    or System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.Forbidden)
                {
                    response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                var upstreamStatus = (int)upstream.StatusCode;
                if (!upstream.IsSuccessStatusCode
                    && upstream.StatusCode != System.Net.HttpStatusCode.NotModified
                    && upstreamStatus != StatusCodes.Status416RangeNotSatisfiable)
                {
                    logger.LogWarning(
                        "Private Cloudinary file delivery returned HTTP {StatusCode}.",
                        upstreamStatus);
                    response.StatusCode = StatusCodes.Status502BadGateway;
                    return;
                }

                response.StatusCode = upstreamStatus;
                response.ContentType = _descriptor.ContentType;
                response.ContentLength = upstream.Content.Headers.ContentLength;
                SetPrivateResponseHeaders(response);
                SetContentDisposition(response, _asAttachment ? "attachment" : "inline");

                CopyResponseHeader(upstream.Headers, response, HeaderNames.AcceptRanges);
                CopyResponseHeader(upstream.Content.Headers, response, HeaderNames.ContentRange);
                CopyResponseHeader(upstream.Headers, response, HeaderNames.ETag);
                CopyResponseHeader(upstream.Content.Headers, response, HeaderNames.LastModified);

                if (upstream.StatusCode == System.Net.HttpStatusCode.NotModified)
                    return;

                await using var upstreamStream = await upstream.Content.ReadAsStreamAsync(
                    httpContext.RequestAborted);
                await upstreamStream.CopyToAsync(
                    response.Body,
                    httpContext.RequestAborted);
            }
        }

        private void SetPrivateResponseHeaders(HttpResponse response)
        {
            response.Headers[HeaderNames.CacheControl] = _cacheControl;
            response.Headers["X-Content-Type-Options"] = "nosniff";
        }

        private void SetContentDisposition(HttpResponse response, string dispositionType)
        {
            var contentDisposition = new ContentDispositionHeaderValue(dispositionType);
            contentDisposition.SetHttpFileName(_descriptor.DownloadFileName);
            response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
        }

        private static void CopyResponseHeader(
            HttpHeaders source,
            HttpResponse response,
            string headerName)
        {
            if (source.TryGetValues(headerName, out var values))
                response.Headers[headerName] = values.ToArray();
        }
    }
}

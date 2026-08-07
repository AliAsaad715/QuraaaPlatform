using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Quraaa.API.Requests.Purchases;
using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
using Quraaa.Application.Features.Purchases.Queries.GetPurchaseDigitalAsset;
using Quraaa.Application.Features.Purchases.Queries.GetSellHistory;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/purchases")]
    public class PurchaseHistoryController : ApiClientController
    {
        [HttpGet("me/buy-history")]
        [ProducesResponseType(typeof(PagedResult<BuyHistoryItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBuyHistory(
            [FromQuery] GetBuyHistoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetBuyHistoryQuery(
                userId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("me/sell-history")]
        [ProducesResponseType(typeof(PagedResult<SellHistoryItemResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellHistory(
            [FromQuery] GetSellHistoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetSellHistoryQuery(
                userId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // ── GET /api/purchases/{purchaseId}/stream ────────────────────────────
        /// <summary>
        /// Streams the digital asset for a purchase the caller owns, for secure
        /// in-app rendering (e.g. a PDF viewer) rather than a browser download.
        /// </summary>
        /// <remarks>
        /// - Supports HTTP Range requests (<c>206 Partial Content</c>), so in-app PDF
        ///   viewers can stream and seek without fetching the whole file first.
        /// - Cache validation: responses carry <c>ETag</c> and <c>Last-Modified</c>.
        ///   A request with a matching <c>If-None-Match</c> or <c>If-Modified-Since</c>
        ///   gets back <c>304 Not Modified</c> with no body.
        /// - <c>Cache-Control: private, no-transform, max-age=3600</c> allows the
        ///   caller's own client to cache the response but blocks shared/proxy caches,
        ///   since the asset is only authorized for the requesting user.
        /// - <c>Content-Disposition: inline</c> keeps the response viewable in-browser
        ///   instead of forcing a download prompt.
        /// - A missing purchase and one that belongs to another user both return 404,
        ///   so this endpoint cannot be used to enumerate valid purchase IDs.
        /// </remarks>
        [HttpGet("{purchaseId:guid}/stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StreamDigitalAsset(
            [FromRoute] Guid purchaseId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetPurchaseDigitalAssetQuery(purchaseId, userId),
                cancellationToken);

            return HandleResult(result, descriptor =>
            {
                // Sensitive, per-user content: caller may cache it, but shared/proxy
                // caches must not (hence "private", not "public").
                Response.Headers[HeaderNames.CacheControl] = "private, no-transform, max-age=3600";

                // Set manually (rather than via PhysicalFile's fileDownloadName) so the
                // disposition stays "inline" — PhysicalFileResult forces "attachment"
                // whenever a download name is supplied, which would block in-browser
                // PDF viewing instead of allowing it.
                var contentDisposition = new ContentDispositionHeaderValue("inline");
                contentDisposition.SetHttpFileName(descriptor.DownloadFileName);
                Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

                // Passing lastModified/entityTag makes PhysicalFileResult itself compare
                // them against If-Modified-Since/If-None-Match and short-circuit with
                // 304 Not Modified — no manual conditional-request handling needed here.
                return PhysicalFile(
                    descriptor.PhysicalPath,
                    descriptor.ContentType,
                    lastModified: descriptor.LastModifiedUtc,
                    entityTag: new EntityTagHeaderValue(descriptor.ETag),
                    enableRangeProcessing: true);
            });
        }
    }
}
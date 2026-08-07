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

        // ── GET /api/purchases/{purchaseId}/download ─────────────────────────
        /// <summary>
        /// Streams the digital asset for a purchase the caller owns.
        /// </summary>
        /// <remarks>
        /// Supports HTTP Range requests (206 Partial Content), so browser PDF
        /// viewers can stream and seek without downloading the whole file first.
        /// A missing purchase and one that belongs to another user both return 404,
        /// so this endpoint cannot be used to enumerate valid purchase IDs.
        /// </remarks>
        [HttpGet("{purchaseId:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadDigitalAsset(
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
                // Set manually (rather than via PhysicalFile's fileDownloadName) so the
                // disposition stays "inline" — PhysicalFileResult forces "attachment"
                // whenever a download name is supplied, which would block in-browser
                // PDF viewing instead of allowing it.
                var contentDisposition = new ContentDispositionHeaderValue("inline");
                contentDisposition.SetHttpFileName(descriptor.DownloadFileName);
                Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

                return PhysicalFile(
                    descriptor.PhysicalPath,
                    descriptor.ContentType,
                    enableRangeProcessing: true);
            });
        }
    }
}
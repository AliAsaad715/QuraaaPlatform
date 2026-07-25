using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Purchases;
using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
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
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Books;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Queries.GetHomePageCatalog;
using Quraaa.Application.Features.Books.Queries.GetMostPopularBooks;
using Quraaa.Application.Features.Books.Queries.GetRecommendedBooks;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    public class BooksController : ApiClientController
    {
        /// <summary>
        /// Retrieves the paginated home page catalog with optional filtering and sorting.
        /// </summary>
        /// <param name="request">Catalog query parameters including search terms, category, library, seller type, price limits, format, and condition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// ### Allowed Enum Values
        ///
        /// **SellerType (`SellerType`):**
        /// - `1` or `Library`: Listings published by a library/publisher
        /// - `2` or `User`: Listings published by an individual user
        ///
        /// **Format (`ListingFormat`):**
        /// - `1` or `Digital`: Digital book asset (e.g., PDF/ePub)
        /// - `2` or `Physical`: Hard copy physical book
        ///
        /// **Condition (`BookCondition`):**
        /// - `1` or `New`: Brand new, unread condition
        /// - `2` or `LikeNew`: Minimal to no signs of wear
        /// - `3` or `Good`: Minor wear, completely intact
        /// - `4` or `Acceptable`: Noticeable wear, fully readable
        /// </remarks>
        [AllowAnonymous]
        [HttpGet("home-catalog")]
        [ProducesResponseType(typeof(PagedResult<HomeBookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHomePageCatalog(
            [FromQuery] GetHomePageCatalogRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new GetHomePageCatalogQuery(
                request.SearchTerm,
                request.CategoryId,
                request.LibraryId,
                request.SellerType,
                request.Format,
                request.IsFree,
                request.Condition,
                request.MinPrice,
                request.MaxPrice,
                request.SortBy,
                request.PageNumber,
                request.PageSize);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpGet("recommended")]
        [ProducesResponseType(typeof(PagedResult<PopularBookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRecommendedBooks(
            [FromQuery] GetRecommendedBooksRequest request,
            [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetRecommendedBooksQuery(
                userId,
                acceptLanguage?.Trim().ToLowerInvariant() ?? string.Empty,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpGet("most-popular")]
        [ProducesResponseType(typeof(PagedResult<PopularBookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMostPopularBooks(
            [FromQuery] GetMostPopularBooksRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new GetMostPopularBooksQuery(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy,
                request.IncludeUnranked);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }
    }
}

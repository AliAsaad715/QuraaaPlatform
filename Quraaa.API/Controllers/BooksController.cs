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

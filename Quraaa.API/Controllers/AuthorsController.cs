using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Authors;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Queries.GetAuthorBooks;
using Quraaa.Application.Features.Authors.Queries.GetPublicAuthorDetails;
using Quraaa.Application.Features.Authors.Queries.SearchAuthors;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Public, read-only author profile endpoints.
    /// </summary>
    public class AuthorsController : ApiClientController
    {
        /// <summary>
        /// Retrieves the public profile of an author.
        /// </summary>
        /// <response code="200">The author was found and returned successfully.</response>
        /// <response code="404">No author exists with the given id.</response>
        [AllowAnonymous]
        [HttpGet("{authorId:guid}")]
        [ProducesResponseType(typeof(PublicAuthorDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuthorDetails(
            [FromRoute] Guid authorId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetPublicAuthorDetailsQuery(authorId),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paged collection of the author's books that currently have at least
        /// one active, in-stock marketplace listing.
        /// </summary>
        /// <remarks>
        /// Books are grouped by catalog book. Each item reports the cheapest active listing,
        /// the available format, seller count, and the book's rating summary. Supported
        /// <c>sortBy</c> values are <c>latest</c>, <c>bestselling</c>, <c>mostpopular</c>,
        /// <c>pricelowtohigh</c>, <c>pricehightolow</c>, and <c>toprated</c>.
        /// </remarks>
        /// <response code="200">A paged collection of the author's available books was returned.</response>
        /// <response code="400">The pagination, search, or sorting input is invalid.</response>
        /// <response code="404">No author exists with the given id.</response>
        [AllowAnonymous]
        [HttpGet("{authorId:guid}/books")]
        [ProducesResponseType(typeof(PagedResult<HomeBookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuthorBooks(
            [FromRoute] Guid authorId,
            [FromQuery] GetAuthorBooksRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAuthorBooksQuery(
                authorId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.SortBy);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Searches for distinct authors by name, returning each match's total book count.
        /// </summary>
        /// <response code="200">A paged collection of matching authors was returned.</response>
        /// <response code="400">The pagination or search input is invalid.</response>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<AuthorSearchResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchAuthors(
            [FromQuery] SearchAuthorsRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new SearchAuthorsQuery(request.SearchTerm, request.PageNumber, request.PageSize);
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }
    }
}

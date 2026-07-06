using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Books;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Queries.GetMostPopularBooks;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    public class BooksController : ApiClientController
    {
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.BookReports;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Queries.GetLibraryBookReports;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// What readers have reported about the books this library sells.
    /// </summary>
    [Route("api/library-admin/book-reports")]
    [Authorize(Roles = nameof(Role.LibraryOwner))]
    public class LibraryBookReportsController : ApiClientController
    {
        // ── GET /api/library-admin/book-reports ──────────────────────────────
        /// <summary>
        /// Reports filed against books this library currently lists, newest
        /// first. Each row carries the reason, the reporter's description, where
        /// the report stands, and whether the book itself is still in the
        /// catalogue. Reporters are never identified.
        /// </summary>
        /// <param name="request">Paging and optional status / book filters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">A paged collection of reports on your books.</response>
        /// <response code="400">The paging or filter values are invalid.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="404">The caller has no approved library.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<LibraryBookReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyBookReports(
            [FromQuery] GetLibraryBookReportsRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetLibraryBookReportsQuery
                {
                    UserId = userId,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Status = request.Status,
                    BookId = request.BookId,
                },
                cancellationToken);

            return HandleResult(result);
        }
    }
}

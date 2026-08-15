using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.BookReports;
using Quraaa.Application.Features.BookReports.Commands.CreateBookReport;
using Quraaa.Application.Features.BookReports.Commands.UpdateBookReportStatus;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Queries.GetBookReportById;
using Quraaa.Application.Features.BookReports.Queries.GetBookReportReasons;
using Quraaa.Application.Features.BookReports.Queries.GetBookReports;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Readers report books that carry inappropriate, incorrect, or
    /// unauthorized content; administrators moderate what comes in.
    /// </summary>
    [Route("api/book-reports")]
    public class BookReportsController : ApiClientController
    {
        // ── GET /api/book-reports/reasons ────────────────────────────────────
        /// <summary>
        /// The predefined reasons to show in the report form, with English and
        /// Arabic labels. A reason flagged RequiresDetails obliges the reporter
        /// to describe the problem.
        /// </summary>
        /// <response code="200">The selectable report reasons.</response>
        [HttpGet("reasons")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyCollection<BookReportReasonResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReasons(CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetBookReportReasonsQuery(), cancellationToken);

            return HandleResult(result);
        }

        // ── POST /api/books/{bookId}/reports ─────────────────────────────────
        /// <summary>
        /// Submits a report against a book. A reader may report a given book
        /// once; a second attempt is rejected as a conflict.
        /// </summary>
        /// <param name="bookId">The reported book.</param>
        /// <param name="command">The reason and optional details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="201">The report was submitted and is awaiting review.</response>
        /// <response code="400">The reason is unknown, or details are missing for reason Other.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="404">The book or the reporting user was not found.</response>
        /// <response code="409">This reader already reported this book.</response>
        [HttpPost("~/api/books/{bookId:guid}/reports")]
        [Authorize(Roles = nameof(Role.User))]
        [ProducesResponseType(typeof(BookReportResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReport(
            [FromRoute] Guid bookId,
            [FromBody] CreateBookReportCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { UserId = userId, BookId = bookId },
                cancellationToken);

            // 201 without a Location header: the report resource is
            // administrator-only, so pointing the reporter at it would be a
            // link they could not follow.
            return HandleResult(
                result,
                report => StatusCode(StatusCodes.Status201Created, report));
        }

        // ── GET /api/book-reports ────────────────────────────────────────────
        /// <summary>
        /// The moderation queue. Defaults to Pending reports, newest first;
        /// pass a status to review any other bucket, or omit it entirely to see
        /// every report. Only accessible by administrators.
        /// </summary>
        /// <response code="200">A paged collection of reports.</response>
        /// <response code="400">The paging or filter values are invalid.</response>
        [HttpGet]
        [Authorize(Roles = nameof(Role.Admin))]
        [ProducesResponseType(typeof(PagedResult<BookReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReports(
            [FromQuery] GetBookReportsRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetBookReportsQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Status = request.Status,
                    BookId = request.BookId,
                    ReporterUserId = request.ReporterUserId,
                    SearchTerm = request.SearchTerm,
                },
                cancellationToken);

            return HandleResult(result);
        }

        // ── GET /api/book-reports/{reportId} ─────────────────────────────────
        /// <summary>
        /// One report in full, for the review screen. Only accessible by
        /// administrators.
        /// </summary>
        /// <response code="200">The report.</response>
        /// <response code="404">No such report.</response>
        [HttpGet("{reportId:guid}")]
        [Authorize(Roles = nameof(Role.Admin))]
        [ProducesResponseType(typeof(BookReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReport(
            [FromRoute] Guid reportId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetBookReportByIdQuery(reportId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── PATCH /api/book-reports/{reportId}/status ────────────────────────
        /// <summary>
        /// Records the outcome of investigating a report: move it to InReview
        /// while looking into it, then Resolved or Rejected. Resolved and
        /// Rejected are final. Only accessible by administrators.
        /// </summary>
        /// <param name="reportId">The report to update.</param>
        /// <param name="command">The new status and an optional moderator note.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The updated report.</response>
        /// <response code="400">The status is missing or cannot be applied.</response>
        /// <response code="404">No such report.</response>
        /// <response code="409">The report was already closed, or changed concurrently.</response>
        [HttpPatch("{reportId:guid}/status")]
        [Authorize(Roles = nameof(Role.Admin))]
        [ProducesResponseType(typeof(BookReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateReportStatus(
            [FromRoute] Guid reportId,
            [FromBody] UpdateBookReportStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { ReportId = reportId, AdminId = adminId },
                cancellationToken);

            return HandleResult(result);
        }
    }
}

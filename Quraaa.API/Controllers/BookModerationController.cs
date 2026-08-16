using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Books;
using Quraaa.Application.Features.Books.Commands.RevertBookToVersion;
using Quraaa.Application.Features.Books.Commands.SetBookVisibility;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Queries.GetBookVersions;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Administrator moderation of catalogue books: their change history, the
    /// ability to restore an earlier version, and catalogue visibility.
    /// </summary>
    [Route("api/books/{bookId:guid}")]
    [Authorize(Roles = nameof(Role.SuperAdmin))]
    public class BookModerationController : ApiClientController
    {
        // ── GET /api/books/{bookId}/versions ─────────────────────────────────
        /// <summary>
        /// The book's full change history, newest first. Every edit and every
        /// revert appears here; nothing is ever removed.
        /// </summary>
        /// <response code="200">The version history.</response>
        /// <response code="404">No such book.</response>
        [HttpGet("versions")]
        [ProducesResponseType(typeof(IReadOnlyCollection<BookVersionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVersions(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetBookVersionsQuery(bookId), cancellationToken);

            return HandleResult(result);
        }

        // ── POST /api/books/{bookId}/revert ──────────────────────────────────
        /// <summary>
        /// Restores the content of an earlier version. The old content is copied
        /// forward as a new version, so the history stays intact and the revert
        /// is itself auditable and reversible.
        /// </summary>
        /// <response code="200">The book's moderation state after the revert.</response>
        /// <response code="400">The version number is not an earlier version.</response>
        /// <response code="404">No such book or version.</response>
        /// <response code="409">The book changed concurrently; retry.</response>
        [HttpPost("revert")]
        [ProducesResponseType(typeof(BookModerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RevertToVersion(
            [FromRoute] Guid bookId,
            [FromBody] RevertBookVersionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RevertBookToVersionCommand
                {
                    BookId = bookId,
                    AdminId = adminId,
                    VersionNumber = request.VersionNumber,
                    ModerationNote = request.ModerationNote,
                },
                cancellationToken);

            return HandleResult(result);
        }

        // ── PUT /api/books/{bookId}/visibility ───────────────────────────────
        /// <summary>
        /// Withholds a book from the catalogue, or returns it. Books withheld
        /// automatically by reports are restored the same way.
        /// </summary>
        /// <response code="200">The book's moderation state after the change.</response>
        /// <response code="404">No such book.</response>
        /// <response code="409">The book changed concurrently; retry.</response>
        [HttpPut("visibility")]
        [ProducesResponseType(typeof(BookModerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetVisibility(
            [FromRoute] Guid bookId,
            [FromBody] SetBookVisibilityRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetBookVisibilityCommand
                {
                    BookId = bookId,
                    AdminId = adminId,
                    Hidden = request.Hidden,
                    ModerationNote = request.ModerationNote,
                },
                cancellationToken);

            return HandleResult(result);
        }
    }
}

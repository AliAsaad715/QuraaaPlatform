using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Files;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.RegisterLibrary;
using Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Queries.GetLibraries;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests;
using Quraaa.Application.Features.Libraries.Queries.GetMyProfile;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    public class LibrariesController : ApiClientController
    {
        [Authorize]
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LibraryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Register([FromForm] RegisterLibraryRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var command = new RegisterLibraryCommand(
                request.LibraryName,
                request.Location,
                request.LibraryImage is null ? null : new FormFileUploadedFile(request.LibraryImage),
                request.HeaderImage is null ? null : new FormFileUploadedFile(request.HeaderImage),
                request.Email,
                userId
            );

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PublicLibraryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLibraries([FromQuery] GetLibrariesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get a paged list of books available in a specific library.
        /// </summary>
        /// <param name="libraryId" example="01f185c0-dff4-45fa-8fe6-60d1c870ea8b">The unique identifier of the library (Pre-loaded example containing books for testing(FrontEnd)).</param>
        /// <param name="request">Pagination, filtering, and sorting parameters.</param>
        /// <param name="cancellationToken"></param>
        [AllowAnonymous]
        [HttpGet("{libraryId}/books")]
        [ProducesResponseType(typeof(PagedResult<ListingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLibraryBooks(
        [FromRoute] Guid libraryId,
        [FromQuery] GetLibraryBooksRequest request,
        CancellationToken cancellationToken = default)
        {
            var query = new GetLibraryBooksQuery
            {
                LibraryId = libraryId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves the profile of the currently authenticated library owner.
        /// </summary>
        /// <response code="200">The profile was found and returned successfully.</response>
        /// <response code="404">No profile exists for the authenticated user.</response>
        [HttpGet("my-profile")]
        [Authorize(Roles = "LibraryOwner")]
        [ProducesResponseType(typeof(MyProfileLibraryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paged list of library registration requests. Only accessible by administrators.
        /// </summary>
        /// <param name="request">
        /// Pagination, filtering, and sorting parameters.
        /// Filter by status using the LibraryApprovalStatus enum (1=Pending, 2=Approved, 3=Rejected).
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// An IActionResult containing a paged result of LibraryRequestResponse objects.
        /// </returns>
        /// <remarks>
        /// LibraryApprovalStatus values:
        /// - Pending (1): Awaiting admin review.
        /// - Approved (2): Request has been approved.
        /// - Rejected (3): Request has been rejected.
        /// </remarks>
        /// <response code="200">A paged collection of library requests was returned successfully.</response>
        [HttpGet("requests")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<LibraryRequestResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequests(
            [FromQuery] GetLibraryRequestsQuery request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(request, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates the approval status of a library by only admin.
        /// </summary>
        /// <param name="id">The library identifier.</param>
        /// <returns>Returns the result of the update operation.</returns>
        /// <response code="200">Approval status updated successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Library not found.</response>
        [HttpPatch("{id:guid}/approval-status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateApprovalStatus(
            [FromRoute] Guid id,
            [FromBody] UpdateLibraryApprovalStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { LibraryId = id, AdminId = adminId },
                cancellationToken);

            return HandleResult(result);
        }
    }
}

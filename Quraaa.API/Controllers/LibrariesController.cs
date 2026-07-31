using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Files;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.RegisterLibrary;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Queries.GetLibraries;
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
        /// Get a paged list of books available in a specific library.
        /// </summary>
        /// <param name="libraryId" example="01f185c0-dff4-45fa-8fe6-60d1c870ea8b">The unique identifier of the library (Pre-loaded example containing books for testing(FrontEnd)).</param>
        /// <param name="request">Pagination, filtering, and sorting parameters.</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("my-profile")]
        [Authorize(Roles = "LibraryOwner")]
        [ProducesResponseType(typeof(PagedResult<MyProfileLibraryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    }
}

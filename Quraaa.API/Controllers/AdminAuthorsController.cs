using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Authors.Commands.CreateAuthor;
using Quraaa.Application.Features.Authors.Commands.UpdateAuthor;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Queries.GetAuthorById;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Admin-only management of catalog authors.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/authors")]
    public class AdminAuthorsController : ApiClientController
    {
        /// <summary>
        /// Creates a new author.
        /// </summary>
        /// <response code="200">The author was created successfully.</response>
        /// <response code="400">The request failed validation.</response>
        [HttpPost]
        [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAuthor(
            [FromBody] CreateAuthorCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a single author by id.
        /// </summary>
        /// <response code="200">The author was found and returned successfully.</response>
        /// <response code="404">No author exists with the given id.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AuthorDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuthorById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetAuthorByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates an existing author's name, bio, photo, and birth date.
        /// </summary>
        /// <response code="200">The author was updated successfully.</response>
        /// <response code="400">The request failed validation.</response>
        /// <response code="404">No author exists with the given id.</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(AuthorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAuthor(
            [FromRoute] Guid id,
            [FromBody] UpdateAuthorCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { Id = id, ModifiedBy = adminId },
                cancellationToken);

            return HandleResult(result);
        }

    }
}

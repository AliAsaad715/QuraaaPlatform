using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Comments;
using Quraaa.Application.Features.Comments.Commands.AddComment;
using Quraaa.Application.Features.Comments.Commands.DeleteComment;
using Quraaa.Application.Features.Comments.Commands.UpdateComment;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Features.Comments.Queries.GetBookComments;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    [Route("api/books/{bookId:guid}/comments")]
    public class CommentsController : ApiClientController
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<CommentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookComments(
            [FromRoute] Guid bookId,
            [FromQuery] GetBookCommentsRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new GetBookCommentsQuery(bookId, request.PageNumber, request.PageSize);
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.User))]
        [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddComment(
            [FromRoute] Guid bookId,
            [FromBody] AddCommentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new AddCommentCommand(userId, bookId, request.Content),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPut("{commentId:guid}")]
        [Authorize(Roles = nameof(Role.User))]
        [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateComment(
            [FromRoute] Guid bookId,
            [FromRoute] Guid commentId,
            [FromBody] UpdateCommentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new UpdateCommentCommand(commentId, userId, bookId, request.Content),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete("{commentId:guid}")]
        [Authorize(Roles = nameof(Role.User))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteComment(
            [FromRoute] Guid bookId,
            [FromRoute] Guid commentId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteCommentCommand(commentId, userId, bookId),
                cancellationToken);

            return HandleResult(result);
        }
    }
}

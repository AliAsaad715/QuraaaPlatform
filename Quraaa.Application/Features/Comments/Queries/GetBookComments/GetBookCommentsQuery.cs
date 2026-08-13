using MediatR;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Comments.Queries.GetBookComments
{
    public record GetBookCommentsQuery(
        Guid BookId,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<AppResult<PagedResult<CommentResponse>>>;
}

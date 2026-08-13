using MediatR;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Ratings.Commands.RateBook
{
    public record RateBookCommand(Guid UserId, Guid BookId, int Score) : IRequest<AppResult<BookRatingResponse>>;
}

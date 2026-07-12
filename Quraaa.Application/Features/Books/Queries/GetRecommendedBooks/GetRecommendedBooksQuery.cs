using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Books.Queries.GetRecommendedBooks
{
    public record GetRecommendedBooksQuery(
        Guid UserId,
        string Language,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null) : IRequest<AppResult<PagedResult<PopularBookResponse>>>;
}

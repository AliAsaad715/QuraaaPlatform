using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.Books.Queries.GetRecommendedBooks
{
    public record GetRecommendedBooksQuery(
        Guid UserId,
        Language? Language,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null) : IRequest<AppResult<PagedResult<PopularBookResponse>>>;
}

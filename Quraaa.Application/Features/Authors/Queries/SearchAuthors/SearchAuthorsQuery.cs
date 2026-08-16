using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Queries.SearchAuthors
{
    public record SearchAuthorsQuery(string? SearchTerm, int PageNumber = 1, int PageSize = 10)
        : IRequest<AppResult<PagedResult<AuthorSearchResponse>>>;
}

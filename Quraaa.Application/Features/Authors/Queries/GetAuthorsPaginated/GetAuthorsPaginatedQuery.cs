using MediatR;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Shared.Requests;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorsPaginated
{
    public record GetAuthorsPaginatedQuery : PaginationRequestDTO, IRequest<AppResult<PagedResult<AuthorResponse>>>
    {
        public string? SearchTerm { get; init; }
    }
}

using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Queries.SearchLibraries
{
    public record SearchLibrariesQuery(string? SearchTerm, int PageNumber = 1, int PageSize = 10)
        : IRequest<AppResult<PagedResult<LibrarySearchResponse>>>;
}

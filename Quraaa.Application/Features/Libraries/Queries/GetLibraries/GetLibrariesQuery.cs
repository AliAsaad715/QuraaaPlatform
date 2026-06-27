using MediatR;
using Quraaa.Application.Shared.Requests;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraries
{
    public record GetLibrariesQuery : PaginationRequestDTO, IRequest<AppResult<PagedResult<PublicLibraryResponse>>>
    {
        public string? SearchTerm { get; init; } = null;
    }
}
using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraries
{
    public record GetLibrariesQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<AppResult<PagedResult<PublicLibraryResponse>>>;
}
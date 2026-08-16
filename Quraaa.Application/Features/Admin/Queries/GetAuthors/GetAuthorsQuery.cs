using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Admin.Queries.GetAuthors
{
    /// <summary>
    /// Administrator query: the author list. Deactivated records are
    /// hidden unless explicitly requested, so the default view matches what the
    /// rest of the platform sees.
    /// </summary>
    public record GetAuthorsQuery : IRequest<AppResult<PagedResult<AdminAuthorResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? SearchTerm { get; init; }
        public bool IncludeDeactivated { get; init; }
    }
}

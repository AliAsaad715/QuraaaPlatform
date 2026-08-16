using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Queries.GetUsers
{
    /// <summary>
    /// Administrator query: the user list. Deactivated records are
    /// hidden unless explicitly requested, so the default view matches what the
    /// rest of the platform sees.
    /// </summary>
    public record GetUsersQuery : IRequest<AppResult<PagedResult<AdminUserResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? SearchTerm { get; init; }
        public bool IncludeDeactivated { get; init; }

        /// <summary>Narrow to a single role.</summary>
        public Role? Role { get; init; }
    }
}

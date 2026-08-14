using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Admin.Queries.GetAdminDashboardStats
{
    public record GetAdminDashboardStatsQuery : IRequest<AppResult<AdminDashboardStatsDto>>;
}

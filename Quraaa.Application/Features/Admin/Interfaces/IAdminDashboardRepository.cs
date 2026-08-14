using Quraaa.Application.Features.Admin.Common;

namespace Quraaa.Application.Features.Admin.Interfaces
{
    public interface IAdminDashboardRepository
    {
        Task<AdminDashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
    }
}

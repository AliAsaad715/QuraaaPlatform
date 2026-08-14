using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Queries.GetAdminDashboardStats
{
    public class GetAdminDashboardStatsQueryHandler
        : BaseApplicationService<GetAdminDashboardStatsQueryHandler>,
          IRequestHandler<GetAdminDashboardStatsQuery, AppResult<AdminDashboardStatsDto>>
    {
        // Admin dashboard tolerates a short staleness window in exchange for
        // sparing the database from a CountAsync fan-out on every refresh.
        private const string CacheKey = "Admin:DashboardStats";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly IAdminDashboardRepository _adminDashboardRepository;
        private readonly IMemoryCache _memoryCache;

        public GetAdminDashboardStatsQueryHandler(
            IAdminDashboardRepository adminDashboardRepository,
            IMemoryCache memoryCache,
            ILogger<GetAdminDashboardStatsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _adminDashboardRepository = adminDashboardRepository;
            _memoryCache = memoryCache;
        }

        public async Task<AppResult<AdminDashboardStatsDto>> Handle(
            GetAdminDashboardStatsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // GetOrCreateAsync returns TItem? defensively (a cache key could in
                // principle hold a stored null); this key is only ever populated by
                // GetStatsAsync, which never returns null.
                var stats = await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    return await _adminDashboardRepository.GetStatsAsync(cancellationToken);
                });

                return stats!;
            }, "Dashboard statistics retrieved successfully.");
        }
    }
}

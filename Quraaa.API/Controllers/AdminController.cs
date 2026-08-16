using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Queries.GetAdminDashboardStats;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = nameof(Role.SuperAdmin))]
    public class AdminController : ApiClientController
    {
        /// <summary>
        /// Retrieves aggregated platform statistics for the admin dashboard:
        /// library, catalog, user, and order counts. The result is cached briefly
        /// server-side so frequent dashboard refreshes stay fast.
        /// </summary>
        [HttpGet("dashboard/stats")]
        [ProducesResponseType(typeof(AdminDashboardStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetAdminDashboardStatsQuery(), cancellationToken);
            return HandleResult(result);
        }
    }
}

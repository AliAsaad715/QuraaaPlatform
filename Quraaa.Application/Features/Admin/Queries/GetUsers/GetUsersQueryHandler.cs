using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Queries.GetUsers
{
    public class GetUsersQueryHandler
        : BaseApplicationService<GetUsersQueryHandler>,
          IRequestHandler<GetUsersQuery, AppResult<PagedResult<AdminUserResponse>>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public GetUsersQueryHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<GetUsersQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<PagedResult<AdminUserResponse>>> Handle(
            GetUsersQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetUsersQuery, PagedResult<AdminUserResponse>>(request, async () =>
            {
                var (items, totalCount) = await _moderationRepository.GetUsersAsync(
                    request.SearchTerm,
                    request.Role,
                    request.IncludeDeactivated,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<AdminUserResponse>(
                    items,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Users retrieved successfully");
        }
    }
}

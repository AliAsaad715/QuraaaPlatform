using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Queries.GetLibraries
{
    public class GetLibrariesQueryHandler
        : BaseApplicationService<GetLibrariesQueryHandler>,
          IRequestHandler<GetLibrariesQuery, AppResult<PagedResult<AdminLibraryResponse>>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public GetLibrariesQueryHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<GetLibrariesQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<PagedResult<AdminLibraryResponse>>> Handle(
            GetLibrariesQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetLibrariesQuery, PagedResult<AdminLibraryResponse>>(request, async () =>
            {
                var (items, totalCount) = await _moderationRepository.GetLibrariesAsync(
                    request.SearchTerm,
                    request.IncludeDeactivated,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<AdminLibraryResponse>(
                    items,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Libraries retrieved successfully");
        }
    }
}

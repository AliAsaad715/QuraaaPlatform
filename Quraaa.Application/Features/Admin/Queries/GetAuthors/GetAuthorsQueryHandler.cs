using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Admin.Queries.GetAuthors
{
    public class GetAuthorsQueryHandler
        : BaseApplicationService<GetAuthorsQueryHandler>,
          IRequestHandler<GetAuthorsQuery, AppResult<PagedResult<AdminAuthorResponse>>>
    {
        private readonly IAdminModerationRepository _moderationRepository;

        public GetAuthorsQueryHandler(
            IAdminModerationRepository moderationRepository,
            ILogger<GetAuthorsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult<PagedResult<AdminAuthorResponse>>> Handle(
            GetAuthorsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetAuthorsQuery, PagedResult<AdminAuthorResponse>>(request, async () =>
            {
                var (items, totalCount) = await _moderationRepository.GetAuthorsAsync(
                    request.SearchTerm,
                    request.IncludeDeactivated,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<AdminAuthorResponse>(
                    items,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Authors retrieved successfully");
        }
    }
}

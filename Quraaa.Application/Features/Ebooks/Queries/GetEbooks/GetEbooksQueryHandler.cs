using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Ebooks.Common;
using Quraaa.Application.Features.Ebooks.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Ebooks.Queries.GetEbooks
{
    public class GetEbooksQueryHandler : BaseApplicationService<GetEbooksQueryHandler>, IRequestHandler<GetEbooksQuery, AppResult<PagedResult<EbookResponse>>>
    {
        private readonly IEbookRepository _ebookRepository;

        public GetEbooksQueryHandler(
            IEbookRepository ebookRepository,
            ILogger<GetEbooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _ebookRepository = ebookRepository;
        }

        public async Task<AppResult<PagedResult<EbookResponse>>> Handle(GetEbooksQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (ebooks, totalCount) = await _ebookRepository.GetPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                return new PagedResult<EbookResponse>(
                    ebooks,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Ebooks retrieved successfully");
        }
    }
}

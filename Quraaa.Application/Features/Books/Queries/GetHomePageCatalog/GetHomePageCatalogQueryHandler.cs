using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Books.Queries.GetHomePageCatalog
{
    public class GetHomePageCatalogQueryHandler
        : BaseApplicationService<GetHomePageCatalogQueryHandler>,
          IRequestHandler<GetHomePageCatalogQuery, AppResult<PagedResult<HomeBookResponse>>>
    {
        private readonly IHomeCatalogRepository _homeCatalogRepository;

        public GetHomePageCatalogQueryHandler(
            IHomeCatalogRepository homeCatalogRepository,
            ILogger<GetHomePageCatalogQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _homeCatalogRepository = homeCatalogRepository;
        }

        public async Task<AppResult<PagedResult<HomeBookResponse>>> Handle(
            GetHomePageCatalogQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetHomePageCatalogQuery, PagedResult<HomeBookResponse>>(request, async () =>
            {
                var (books, totalCount) = await _homeCatalogRepository.GetCatalogAsync(
                    request.SearchTerm,
                    request.CategoryId,
                    request.LibraryId,
                    request.Format,
                    request.IsFree,
                    request.Condition,
                    request.MinPrice,
                    request.MaxPrice,
                    request.SortBy,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<HomeBookResponse>(
                    books,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Home page catalog retrieved successfully");
        }
    }
}

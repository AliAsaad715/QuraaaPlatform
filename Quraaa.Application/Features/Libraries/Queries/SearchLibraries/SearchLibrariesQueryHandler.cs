using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Queries.SearchLibraries
{
    public class SearchLibrariesQueryHandler
        : BaseApplicationService<SearchLibrariesQueryHandler>,
          IRequestHandler<SearchLibrariesQuery, AppResult<PagedResult<LibrarySearchResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public SearchLibrariesQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<SearchLibrariesQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<LibrarySearchResponse>>> Handle(
            SearchLibrariesQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SearchLibrariesQuery, PagedResult<LibrarySearchResponse>>(request, async () =>
            {
                var (items, totalCount) = await _libraryRepository.SearchAsync(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<LibrarySearchResponse>(items, request.PageNumber, request.PageSize, totalCount);
            }, "Libraries retrieved successfully");
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authors.Queries.SearchAuthors
{
    public class SearchAuthorsQueryHandler
        : BaseApplicationService<SearchAuthorsQueryHandler>,
          IRequestHandler<SearchAuthorsQuery, AppResult<PagedResult<AuthorSearchResponse>>>
    {
        private readonly IAuthorRepository _authorRepository;

        public SearchAuthorsQueryHandler(
            IAuthorRepository authorRepository,
            ILogger<SearchAuthorsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<PagedResult<AuthorSearchResponse>>> Handle(
            SearchAuthorsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SearchAuthorsQuery, PagedResult<AuthorSearchResponse>>(request, async () =>
            {
                var (items, totalCount) = await _authorRepository.SearchAsync(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<AuthorSearchResponse>(items, request.PageNumber, request.PageSize, totalCount);
            }, "Authors retrieved successfully");
        }
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorsPaginated
{
    public class GetAuthorsPaginatedQueryHandler
        : BaseApplicationService<GetAuthorsPaginatedQueryHandler>,
          IRequestHandler<GetAuthorsPaginatedQuery, AppResult<PagedResult<AuthorResponse>>>
    {
        private readonly IAuthorRepository _authorRepository;

        public GetAuthorsPaginatedQueryHandler(
            IAuthorRepository authorRepository,
            ILogger<GetAuthorsPaginatedQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<PagedResult<AuthorResponse>>> Handle(GetAuthorsPaginatedQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetAuthorsPaginatedQuery, PagedResult<AuthorResponse>>(request, async () =>
            {
                var (items, totalCount) = await _authorRepository.GetPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                var responses = items
                    .Select(a => new AuthorResponse(a.Id, a.Name, a.Bio, a.PhotoUrl, a.BirthDate, a.CreationTime))
                    .ToList();

                return new PagedResult<AuthorResponse>(responses, request.PageNumber, request.PageSize, totalCount);
            }, "Authors retrieved successfully");
        }
    }
}

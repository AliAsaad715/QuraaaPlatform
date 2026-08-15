using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorBooks
{
    public sealed class GetAuthorBooksQueryHandler
        : BaseApplicationService<GetAuthorBooksQueryHandler>,
          IRequestHandler<GetAuthorBooksQuery, AppResult<PagedResult<HomeBookResponse>>>
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IHomeCatalogRepository _homeCatalogRepository;

        public GetAuthorBooksQueryHandler(
            IAuthorRepository authorRepository,
            IHomeCatalogRepository homeCatalogRepository,
            ILogger<GetAuthorBooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
            _homeCatalogRepository = homeCatalogRepository;
        }

        public async Task<AppResult<PagedResult<HomeBookResponse>>> Handle(
            GetAuthorBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetAuthorBooksQuery, PagedResult<HomeBookResponse>>(
                request,
                async () =>
                {
                    if (!await _authorRepository.ExistsAsync(request.AuthorId, cancellationToken))
                    {
                        throw new NotFoundException(
                            $"Author with ID {request.AuthorId} was not found.");
                    }

                    var (books, totalCount) = await _homeCatalogRepository.GetByAuthorAsync(
                        request.AuthorId,
                        request.SearchTerm,
                        request.SortBy,
                        request.PageNumber,
                        request.PageSize,
                        cancellationToken);

                    return new PagedResult<HomeBookResponse>(
                        books,
                        request.PageNumber,
                        request.PageSize,
                        totalCount);
                },
                "Author books retrieved successfully");
        }
    }
}

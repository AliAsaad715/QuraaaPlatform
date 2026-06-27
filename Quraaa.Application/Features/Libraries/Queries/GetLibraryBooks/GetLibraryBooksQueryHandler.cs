using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryBooks
{
    public class GetLibraryBooksQueryHandler
    : BaseApplicationService<GetLibraryBooksQueryHandler>,
      IRequestHandler<GetLibraryBooksQuery, AppResult<PagedResult<LibraryBookResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLibraryBooksQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetLibraryBooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<LibraryBookResponse>>> Handle(
            GetLibraryBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                if (!await _libraryRepository.ExistsByIdAsync(request.LibraryId, cancellationToken))
                    throw new NotFoundException("Library not found");

                var (books, totalCount) = await _libraryRepository.GetLibraryBooksAsync(
                    request.LibraryId,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDescending,
                    cancellationToken);

                return new PagedResult<LibraryBookResponse>(
                    books,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);

            }, "Library books retrieved successfully");
        }
    }
}

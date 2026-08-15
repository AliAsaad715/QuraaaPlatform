using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Books.Queries.GetBookVersions
{
    public class GetBookVersionsQueryHandler
        : BaseApplicationService<GetBookVersionsQueryHandler>,
          IRequestHandler<GetBookVersionsQuery, AppResult<IReadOnlyCollection<BookVersionResponse>>>
    {
        private readonly IBookVersionRepository _bookVersionRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetBookVersionsQueryHandler(
            IBookVersionRepository bookVersionRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetBookVersionsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookVersionRepository = bookVersionRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<IReadOnlyCollection<BookVersionResponse>>> Handle(
            GetBookVersionsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookVersionsQuery, IReadOnlyCollection<BookVersionResponse>>(request, async () =>
            {
                var book = await _bookVersionRepository.GetBookForUpdateAsync(
                    request.BookId,
                    cancellationToken);

                if (book is null)
                {
                    throw new NotFoundException("Book was not found.");
                }

                var versions = await _bookVersionRepository.GetVersionsAsync(
                    request.BookId,
                    cancellationToken);

                return versions
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new BookVersionResponse(
                        version.VersionNumber,
                        version.Reason,
                        version.RevertedFromVersionNumber,
                        version.ChangedByUserId,
                        version.Title,
                        version.AuthorId,
                        version.Description,
                        _imageUrlFormatter.Format(version.CoverImageUrl),
                        version.CategoryId,
                        version.Language.ToString(),
                        version.Isbn,
                        version.VersionNumber == book.CurrentVersionNumber,
                        version.CreationTime))
                    .ToList();
            }, "Book versions retrieved successfully");
        }
    }
}

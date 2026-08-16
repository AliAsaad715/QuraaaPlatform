using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Queries.ValidateIsbn
{
    public class ValidateIsbnQueryHandler
        : BaseApplicationService<ValidateIsbnQueryHandler>,
          IRequestHandler<ValidateIsbnQuery, AppResult<IsbnLookupResponse>>
    {
        private readonly IBookMetadataService _bookMetadataService;

        public ValidateIsbnQueryHandler(
            IBookMetadataService bookMetadataService,
            ILogger<ValidateIsbnQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _bookMetadataService = bookMetadataService;
        }

        public async Task<AppResult<IsbnLookupResponse>> Handle(
            ValidateIsbnQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<ValidateIsbnQuery, IsbnLookupResponse>(request, async () =>
            {
                // GetBookByIsbnAsync already returns null for both a zero-result lookup
                // (totalItems == 0) and any HTTP/parsing failure — either way, "not found"
                // is the correct answer here, so no extra exception handling is needed.
                var metadata = await _bookMetadataService.GetBookByIsbnAsync(request.Isbn, cancellationToken);
                if (metadata is null)
                {
                    throw new NotFoundException("No book found for the provided ISBN.");
                }

                return new IsbnLookupResponse(
                    request.Isbn,
                    metadata.Title,
                    NullIfEmpty(metadata.Authors),
                    NullIfEmpty(metadata.Publisher),
                    NullIfEmpty(metadata.PublishedDate),
                    NullIfEmpty(metadata.Description),
                    NullIfEmpty(metadata.ThumbnailUrl),
                    NullIfEmpty(metadata.Language),
                    metadata.PageCount);

            }, "ISBN validation completed.");
        }

        private static string? NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Listings.Queries.ValidateIsbn
{
    public class ValidateIsbnQueryHandler
        : BaseApplicationService<ValidateIsbnQueryHandler>,
          IRequestHandler<ValidateIsbnQuery, AppResult<bool>>
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

        public async Task<AppResult<bool>> Handle(
            ValidateIsbnQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // GetBookByIsbnAsync already returns null for both a zero-result lookup
                // (totalItems == 0) and any HTTP/parsing failure — either way, "not found"
                // is the correct answer here, so no extra exception handling is needed.
                var metadata = await _bookMetadataService.GetBookByIsbnAsync(request.Isbn, cancellationToken);
                return metadata is not null;

            }, "ISBN validation completed.");
        }
    }
}

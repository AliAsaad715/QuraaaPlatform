using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryProfitShare
{
    public class GetLibraryProfitShareQueryHandler
        : BaseApplicationService<GetLibraryProfitShareQueryHandler>,
          IRequestHandler<GetLibraryProfitShareQuery, AppResult<LibraryProfitShareResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLibraryProfitShareQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetLibraryProfitShareQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<LibraryProfitShareResponse>> Handle(
            GetLibraryProfitShareQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetLibraryProfitShareQuery, LibraryProfitShareResponse>(request, async () =>
            {
                var library = await _libraryRepository.GetByIdAsync(
                    request.LibraryId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found.");
                }

                return LibraryProfitShareResponse.From(library);
            }, "Library profit share retrieved successfully");
        }
    }
}

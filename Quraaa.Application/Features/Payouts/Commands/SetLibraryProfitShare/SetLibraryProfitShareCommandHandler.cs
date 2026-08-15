using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryProfitShare
{
    public class SetLibraryProfitShareCommandHandler
        : BaseApplicationService<SetLibraryProfitShareCommandHandler>,
          IRequestHandler<SetLibraryProfitShareCommand, AppResult<LibraryProfitShareResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public SetLibraryProfitShareCommandHandler(
            ILibraryRepository libraryRepository,
            ILogger<SetLibraryProfitShareCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<LibraryProfitShareResponse>> Handle(
            SetLibraryProfitShareCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetLibraryProfitShareCommand, LibraryProfitShareResponse>(request, async () =>
            {
                // Any library — not only approved ones — so an administrator
                // can decide the share before approving.
                var library = await _libraryRepository.GetByIdAsync(
                    request.LibraryId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found.");
                }

                var previousPercent = library.ProfitSharePercent;

                library.SetProfitSharePercent(request.ProfitSharePercent, request.AdminId);

                // Optimistic-concurrency conflicts surface from the repository
                // as ConflictException (HTTP 409).
                await _libraryRepository.SaveChangesAsync();

                if (previousPercent != library.ProfitSharePercent)
                {
                    Logger.LogInformation(
                        "Admin {AdminId} changed the profit share of library {LibraryId} from {PreviousPercent}% to {NewPercent}%. Applies to orders paid from now on.",
                        request.AdminId,
                        library.Id,
                        previousPercent,
                        library.ProfitSharePercent);
                }

                return LibraryProfitShareResponse.From(library);
            }, "Library profit share updated successfully");
        }
    }
}

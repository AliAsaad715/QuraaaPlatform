using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.CompleteLibraryWalletOnboarding
{
    public class CompleteLibraryWalletOnboardingCommandHandler
        : BaseApplicationService<CompleteLibraryWalletOnboardingCommandHandler>,
          IRequestHandler<CompleteLibraryWalletOnboardingCommand, AppResult<LibraryWalletResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly LibraryStripeOnboardingService _onboardingService;

        public CompleteLibraryWalletOnboardingCommandHandler(
            ILibraryRepository libraryRepository,
            LibraryStripeOnboardingService onboardingService,
            ILogger<CompleteLibraryWalletOnboardingCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _onboardingService = onboardingService;
        }

        public async Task<AppResult<LibraryWalletResponse>> Handle(
            CompleteLibraryWalletOnboardingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CompleteLibraryWalletOnboardingCommand, LibraryWalletResponse>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                return await _onboardingService.SyncStatusAsync(library, cancellationToken);
            }, "Stripe wallet status synchronized");
        }
    }
}

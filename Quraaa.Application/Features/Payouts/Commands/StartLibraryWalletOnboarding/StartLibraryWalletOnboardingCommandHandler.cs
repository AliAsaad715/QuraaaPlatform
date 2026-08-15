using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.StartLibraryWalletOnboarding
{
    public class StartLibraryWalletOnboardingCommandHandler
        : BaseApplicationService<StartLibraryWalletOnboardingCommandHandler>,
          IRequestHandler<StartLibraryWalletOnboardingCommand, AppResult<LibraryStripeOnboardingResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly LibraryStripeOnboardingService _onboardingService;

        public StartLibraryWalletOnboardingCommandHandler(
            ILibraryRepository libraryRepository,
            LibraryStripeOnboardingService onboardingService,
            ILogger<StartLibraryWalletOnboardingCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _onboardingService = onboardingService;
        }

        public async Task<AppResult<LibraryStripeOnboardingResponse>> Handle(
            StartLibraryWalletOnboardingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<StartLibraryWalletOnboardingCommand, LibraryStripeOnboardingResponse>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                return await _onboardingService.StartAsync(
                    library,
                    request.ReturnUrl,
                    request.RefreshUrl,
                    cancellationToken);
            }, "Stripe wallet onboarding link created");
        }
    }
}

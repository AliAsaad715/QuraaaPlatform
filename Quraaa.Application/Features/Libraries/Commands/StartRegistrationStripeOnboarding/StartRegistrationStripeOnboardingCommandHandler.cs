using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Commands.StartRegistrationStripeOnboarding
{
    public sealed class StartRegistrationStripeOnboardingCommandHandler
        : BaseApplicationService<StartRegistrationStripeOnboardingCommandHandler>,
          IRequestHandler<StartRegistrationStripeOnboardingCommand, AppResult<LibraryStripeOnboardingResponse>>
    {
        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryRepository _libraryRepository;
        private readonly LibraryStripeOnboardingService _onboardingService;

        public StartRegistrationStripeOnboardingCommandHandler(
            LibraryRegistrationSessionService sessionService,
            ILibraryRepository libraryRepository,
            LibraryStripeOnboardingService onboardingService,
            ILogger<StartRegistrationStripeOnboardingCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _sessionService = sessionService;
            _libraryRepository = libraryRepository;
            _onboardingService = onboardingService;
        }

        public async Task<AppResult<LibraryStripeOnboardingResponse>> Handle(
            StartRegistrationStripeOnboardingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<StartRegistrationStripeOnboardingCommand, LibraryStripeOnboardingResponse>(request, async () =>
            {
                var session = await _sessionService.ResolveActiveAsync(
                    request.Token,
                    requireSubmitted: true,
                    cancellationToken);

                var library = await _libraryRepository.GetByUserIdAsync(
                    session.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new UnauthenticatedException();
                }

                if (!library.EmailVerifiedAtUtc.HasValue)
                {
                    throw new ApplicationBusinessException(
                        "Verify the library email before connecting a Stripe wallet.",
                        nameof(StartRegistrationStripeOnboardingCommand.Token));
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

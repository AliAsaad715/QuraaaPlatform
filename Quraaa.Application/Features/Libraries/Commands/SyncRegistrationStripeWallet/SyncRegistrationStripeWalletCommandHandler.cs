using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Services;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Commands.SyncRegistrationStripeWallet
{
    public sealed class SyncRegistrationStripeWalletCommandHandler
        : BaseApplicationService<SyncRegistrationStripeWalletCommandHandler>,
          IRequestHandler<SyncRegistrationStripeWalletCommand, AppResult<LibraryWalletResponse>>
    {
        private readonly LibraryRegistrationSessionService _sessionService;
        private readonly ILibraryRegistrationRepository _registrationRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly LibraryStripeOnboardingService _onboardingService;

        public SyncRegistrationStripeWalletCommandHandler(
            LibraryRegistrationSessionService sessionService,
            ILibraryRegistrationRepository registrationRepository,
            ILibraryRepository libraryRepository,
            LibraryStripeOnboardingService onboardingService,
            ILogger<SyncRegistrationStripeWalletCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _sessionService = sessionService;
            _registrationRepository = registrationRepository;
            _libraryRepository = libraryRepository;
            _onboardingService = onboardingService;
        }

        public async Task<AppResult<LibraryWalletResponse>> Handle(
            SyncRegistrationStripeWalletCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SyncRegistrationStripeWalletCommand, LibraryWalletResponse>(request, async () =>
            {
                // allowCompleted: a second sync after the wizard finished must
                // still answer (idempotent return page).
                var session = await _sessionService.ResolveActiveAsync(
                    request.Token,
                    requireSubmitted: true,
                    cancellationToken,
                    allowCompleted: true);

                var library = await _libraryRepository.GetByUserIdAsync(
                    session.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new UnauthenticatedException();
                }

                var wallet = await _onboardingService.SyncStatusAsync(library, cancellationToken);

                var utcNow = DateTime.UtcNow;

                // The wizard has nothing left to do: close the registration
                // session so its link cannot be reused. Guarded on IsActiveAt
                // because the provider round-trip above can outlive the
                // session, and the wallet is already saved either way.
                if (library.IsStripeWalletActive && session.IsActiveAt(utcNow))
                {
                    session.Complete(utcNow);
                    await _registrationRepository.SaveChangesAsync(cancellationToken);
                }

                return wallet;
            }, "Stripe wallet status synchronized");
        }
    }
}

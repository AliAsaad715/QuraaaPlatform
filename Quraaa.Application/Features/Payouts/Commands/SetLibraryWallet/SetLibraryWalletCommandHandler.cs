using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Features.Payouts.Services;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryWallet
{
    public class SetLibraryWalletCommandHandler
        : BaseApplicationService<SetLibraryWalletCommandHandler>,
          IRequestHandler<SetLibraryWalletCommand, AppResult<LibraryWalletResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IPayoutGateway _payoutGateway;
        private readonly LibraryStripeOnboardingService _onboardingService;

        public SetLibraryWalletCommandHandler(
            ILibraryRepository libraryRepository,
            IPayoutGateway payoutGateway,
            LibraryStripeOnboardingService onboardingService,
            ILogger<SetLibraryWalletCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _payoutGateway = payoutGateway;
            _onboardingService = onboardingService;
        }

        public async Task<AppResult<LibraryWalletResponse>> Handle(
            SetLibraryWalletCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetLibraryWalletCommand, LibraryWalletResponse>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                var stripeAccountId = request.StripeAccountId.Trim();

                // A provider-side failure (PayoutGatewayException) is not a
                // client-input error: it propagates so the controller can map
                // it, instead of becoming a misleading field validation error.
                var account = await _payoutGateway.GetConnectedAccountAsync(
                    stripeAccountId,
                    cancellationToken);

                if (account is null)
                {
                    throw new ApplicationBusinessException(
                        "This Stripe account does not exist or is not connected to the platform.",
                        nameof(SetLibraryWalletCommand.StripeAccountId));
                }

                if (!account.CanReceiveTransfers)
                {
                    throw new ApplicationBusinessException(
                        "This Stripe account cannot receive transfers yet. Complete the Stripe onboarding for the account first.",
                        nameof(SetLibraryWalletCommand.StripeAccountId));
                }

                if (!account.BelongsToLibraryOrIsUnowned(library.Id))
                {
                    // Accounts the platform created through onboarding carry the
                    // owning library in their metadata; never let another
                    // library bind (and later open the dashboard of) one.
                    throw new ApplicationBusinessException(
                        "This Stripe account belongs to another library.",
                        nameof(SetLibraryWalletCommand.StripeAccountId));
                }

                var walletChanged = !string.Equals(
                        library.StripeConnectAccountId,
                        stripeAccountId,
                        StringComparison.Ordinal)
                    || !library.IsStripeWalletActive;

                // Verified above as able to receive transfers -> active now.
                library.ConnectStripeWallet(stripeAccountId, DateTime.UtcNow, request.UserId);
                await _libraryRepository.SaveChangesAsync();

                if (walletChanged)
                {
                    // Parked profit shares can be paid now. A no-op re-save must
                    // NOT reschedule: that would pull payouts out of their
                    // failure backoff and spend transfer attempts.
                    await _onboardingService.ReleaseWaitingPayoutsAsync(
                        library.Id,
                        cancellationToken);
                }

                return LibraryWalletResponse.From(library);
            }, "Stripe wallet saved successfully");
        }
    }
}

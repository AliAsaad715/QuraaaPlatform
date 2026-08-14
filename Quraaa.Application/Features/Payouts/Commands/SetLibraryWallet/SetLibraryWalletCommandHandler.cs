using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Interfaces;
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
        private readonly ISellerPayoutRepository _sellerPayoutRepository;
        private readonly IPayoutGateway _payoutGateway;

        public SetLibraryWalletCommandHandler(
            ILibraryRepository libraryRepository,
            ISellerPayoutRepository sellerPayoutRepository,
            IPayoutGateway payoutGateway,
            ILogger<SetLibraryWalletCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _sellerPayoutRepository = sellerPayoutRepository;
            _payoutGateway = payoutGateway;
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

                PayoutConnectedAccountStatus? account;

                try
                {
                    account = await _payoutGateway.GetConnectedAccountAsync(
                        stripeAccountId,
                        cancellationToken);
                }
                catch (PayoutGatewayException exception)
                {
                    // A Stripe-side outage is not a client-input error: let it
                    // propagate so the controller can answer 503 instead of a
                    // misleading 400 ValidationFailure on the field.
                    Logger.LogWarning(
                        exception,
                        "Stripe wallet verification failed for library {LibraryId}.",
                        library.Id);

                    throw;
                }

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

                library.SetStripeWallet(stripeAccountId, request.UserId);
                await _libraryRepository.SaveChangesAsync();

                // Pull payouts that were parked waiting for a wallet forward so
                // the processor pays them on its next cycle. Best-effort: the
                // wallet is already saved, and parked payouts retry on their
                // own schedule regardless.
                try
                {
                    var rescheduledCount = await _sellerPayoutRepository
                        .ReschedulePendingForLibraryAsync(
                            library.Id,
                            DateTime.UtcNow,
                            cancellationToken);

                    if (rescheduledCount > 0)
                    {
                        Logger.LogInformation(
                            "Rescheduled {PayoutCount} pending payout(s) after library {LibraryId} configured its Stripe wallet.",
                            rescheduledCount,
                            library.Id);
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Logger.LogWarning(
                        exception,
                        "Could not reschedule pending payouts for library {LibraryId} after a wallet change.",
                        library.Id);
                }

                return new LibraryWalletResponse(
                    library.StripeConnectAccountId,
                    library.StripeConnectAccountId is not null);
            }, "Stripe wallet saved successfully");
        }
    }
}

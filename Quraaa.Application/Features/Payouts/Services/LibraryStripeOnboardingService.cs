using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Services
{
    /// <summary>
    /// Self-service Stripe wallet onboarding for library owners, shared by the
    /// registration wizard (registration-token flow) and the owner dashboard
    /// (JWT flow). The owner never handles a Stripe account id: the platform
    /// creates the connected account, sends the owner to Stripe-hosted
    /// onboarding, and confirms readiness when they return.
    /// </summary>
    public sealed class LibraryStripeOnboardingService
    {
        private const string ReturnMarker = "stripe=return";
        private const string RefreshMarker = "stripe=refresh";

        // Must match the command property names so a redirect rejection is
        // reported under the same field as its FluentValidation errors.
        private const string ReturnUrlFieldName = "ReturnUrl";
        private const string RefreshUrlFieldName = "RefreshUrl";

        private readonly IPayoutGateway _payoutGateway;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ISellerPayoutRepository _sellerPayoutRepository;
        private readonly ISellerPayoutDispatchSignal _payoutDispatchSignal;
        private readonly LibraryRegistrationOptions _registrationOptions;
        private readonly ILogger<LibraryStripeOnboardingService> _logger;

        public LibraryStripeOnboardingService(
            IPayoutGateway payoutGateway,
            ILibraryRepository libraryRepository,
            ISellerPayoutRepository sellerPayoutRepository,
            ISellerPayoutDispatchSignal payoutDispatchSignal,
            LibraryRegistrationOptions registrationOptions,
            ILogger<LibraryStripeOnboardingService> logger)
        {
            _payoutGateway = payoutGateway;
            _libraryRepository = libraryRepository;
            _sellerPayoutRepository = sellerPayoutRepository;
            _payoutDispatchSignal = payoutDispatchSignal;
            _registrationOptions = registrationOptions;
            _logger = logger;
        }

        /// <summary>
        /// Starts or resumes Stripe-hosted onboarding for the library's wallet:
        /// creates the connected account on first use (persisted before the
        /// link is issued), then returns a fresh onboarding link. Throws
        /// <see cref="ConflictException"/> when the wallet is already active.
        /// </summary>
        public async Task<LibraryStripeOnboardingResponse> StartAsync(
            LibraryAggregate library,
            string? returnUrl,
            string? refreshUrl,
            CancellationToken cancellationToken)
        {
            if (library.IsStripeWalletActive)
            {
                throw new ConflictException(
                    "This library already has an active Stripe wallet. Remove it first to connect a different account.");
            }

            // Checked here — before any Stripe side effect — and not only in
            // the domain guard, so a rejected library never gets an orphan
            // connected account created at Stripe.
            if (library.ApprovalStatus == LibraryApprovalStatus.Rejected)
            {
                throw new ConflictException(
                    "A rejected library cannot configure a Stripe wallet.");
            }

            var resolvedReturnUrl = ResolveRedirectUrl(returnUrl, ReturnMarker, ReturnUrlFieldName);
            var resolvedRefreshUrl = ResolveRedirectUrl(refreshUrl, RefreshMarker, RefreshUrlFieldName);

            var accountId = library.StripeConnectAccountId
                ?? await CreateAndAttachAccountAsync(library, cancellationToken);

            PayoutOnboardingLink link;

            try
            {
                link = await _payoutGateway.CreateOnboardingLinkAsync(
                    accountId,
                    resolvedReturnUrl,
                    resolvedRefreshUrl,
                    cancellationToken);
            }
            catch (PayoutGatewayException exception) when (exception.IsDefinitiveRejection)
            {
                // The stored account may have been deleted at the provider.
                // Detach it and start over so the owner is not stuck retrying.
                if (await _payoutGateway.GetConnectedAccountAsync(accountId, cancellationToken) is not null)
                {
                    throw;
                }

                _logger.LogWarning(
                    exception,
                    "Stripe account {AccountId} of library {LibraryId} no longer exists; detaching and creating a new one.",
                    accountId,
                    library.Id);

                library.RemoveStripeWallet(library.UserId);
                await _libraryRepository.SaveChangesAsync();

                accountId = await CreateAndAttachAccountAsync(library, cancellationToken);

                link = await _payoutGateway.CreateOnboardingLinkAsync(
                    accountId,
                    resolvedReturnUrl,
                    resolvedRefreshUrl,
                    cancellationToken);
            }

            return new LibraryStripeOnboardingResponse(
                link.Url,
                link.ExpiresAtUtc,
                accountId,
                library.WalletStatus);
        }

        /// <summary>
        /// Re-checks the attached wallet with Stripe and, if it can now receive
        /// transfers, marks it active, releases any profit shares that were
        /// waiting for it, and wakes the payout processor. Returns the
        /// library's wallet after the sync. Safe to call repeatedly.
        /// </summary>
        public async Task<LibraryWalletResponse> SyncStatusAsync(
            LibraryAggregate library,
            CancellationToken cancellationToken)
        {
            if (library.StripeConnectAccountId is null)
            {
                return LibraryWalletResponse.From(library);
            }

            // Active wallets are re-verified too: the provider can disable an
            // account after onboarding, and the owner needs to see that here
            // rather than through failing payouts.
            var account = await _payoutGateway.GetConnectedAccountAsync(
                library.StripeConnectAccountId,
                cancellationToken);

            if (account is null)
            {
                // The account vanished at Stripe (deleted/rejected). Detach so
                // the owner can start over instead of being stuck.
                _logger.LogWarning(
                    "Stripe account {AccountId} attached to library {LibraryId} no longer exists; detaching.",
                    library.StripeConnectAccountId,
                    library.Id);

                library.RemoveStripeWallet(library.UserId);
                await _libraryRepository.SaveChangesAsync();
                return LibraryWalletResponse.From(library);
            }

            if (!account.CanReceiveTransfers)
            {
                if (library.IsStripeWalletActive)
                {
                    library.DeactivateStripeWallet();
                    await _libraryRepository.SaveChangesAsync();

                    _logger.LogWarning(
                        "Stripe wallet {AccountId} of library {LibraryId} can no longer receive transfers.",
                        library.StripeConnectAccountId,
                        library.Id);
                }

                return LibraryWalletResponse.From(library);
            }

            if (library.IsStripeWalletActive)
            {
                return LibraryWalletResponse.From(library);
            }

            library.MarkStripeWalletActive(DateTime.UtcNow);
            await _libraryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Stripe wallet {AccountId} of library {LibraryId} is now active.",
                library.StripeConnectAccountId,
                library.Id);

            await ReleaseWaitingPayoutsAsync(library.Id, cancellationToken);

            return LibraryWalletResponse.From(library);
        }

        /// <summary>
        /// Creates the provider account for this library and persists it BEFORE
        /// any onboarding link is handed out, so a half-finished onboarding can
        /// always be resumed against the same account.
        /// </summary>
        private async Task<string> CreateAndAttachAccountAsync(
            LibraryAggregate library,
            CancellationToken cancellationToken)
        {
            var accountId = await _payoutGateway.CreateConnectedAccountAsync(
                new PayoutConnectedAccountRequest(
                    library.Email,
                    library.LibraryName,
                    library.Id,
                    library.UserId,
                    // ConcurrencyStamp rotates on every library mutation, including
                    // wallet attach/detach: a retry after a transient failure (stamp
                    // unchanged in the DB) dedupes to the same account, while a fresh
                    // start after a detach (e.g. the account vanished at Stripe) gets a
                    // genuinely new one. An unrelated concurrent mutation (admin
                    // approval) between the provider call and our save can at worst
                    // leave one empty, unattached account behind.
                    IdempotencyKey: $"library-connect-account:{library.Id:N}:{library.ConcurrencyStamp:N}"),
                cancellationToken);

            library.ConnectStripeWallet(accountId, activatedAtUtc: null, library.UserId);
            await _libraryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Created Stripe connected account {AccountId} for library {LibraryId}.",
                accountId,
                library.Id);

            return accountId;
        }

        /// <summary>
        /// Pulls forward any payouts parked while the wallet was missing or
        /// incomplete and wakes the processor. Best-effort: parked payouts
        /// retry on their own schedule regardless.
        /// </summary>
        public async Task ReleaseWaitingPayoutsAsync(
            Guid libraryId,
            CancellationToken cancellationToken)
        {
            try
            {
                var rescheduledCount = await _sellerPayoutRepository
                    .ReschedulePendingForLibraryAsync(
                        libraryId,
                        DateTime.UtcNow,
                        cancellationToken);

                if (rescheduledCount > 0)
                {
                    _logger.LogInformation(
                        "Rescheduled {PayoutCount} pending payout(s) for library {LibraryId} after its wallet became available.",
                        rescheduledCount,
                        libraryId);

                    _payoutDispatchSignal.RequestImmediateProcessing();
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Could not reschedule pending payouts for library {LibraryId} after a wallet change.",
                    libraryId);
            }
        }

        /// <summary>
        /// Validates a caller-supplied redirect target against the allow-listed
        /// frontend origins, or falls back to the dashboard registration URL
        /// with a marker query so the SPA knows why it was re-entered.
        /// </summary>
        private Uri ResolveRedirectUrl(string? candidate, string marker, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                var builder = new UriBuilder(_registrationOptions.DashboardRegisterUrl)
                {
                    Query = marker,
                };

                return builder.Uri;
            }

            if (!Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new ApplicationBusinessException(
                    "The redirect URL must be an absolute http(s) URL without embedded credentials.",
                    parameterName);
            }

            // AllowedReturnOrigins always contains the dashboard origin (see
            // the composition root), so it is the single allow-list.
            var origin = uri.GetLeftPart(UriPartial.Authority);

            if (!_registrationOptions.AllowedReturnOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                throw new ApplicationBusinessException(
                    "The redirect URL must belong to one of the platform's frontends.",
                    parameterName);
            }

            return uri;
        }
    }
}

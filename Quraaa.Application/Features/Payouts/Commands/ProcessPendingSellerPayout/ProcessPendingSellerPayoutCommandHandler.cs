using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Domain.Library;
using Quraaa.Domain.Payouts;
using Quraaa.Domain.Payouts.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.ProcessPendingSellerPayout
{
    public sealed class ProcessPendingSellerPayoutCommandHandler
        : IRequestHandler<ProcessPendingSellerPayoutCommand, bool>
    {
        /// <summary>
        /// Stripe retains idempotency results for at least 24 hours. Beyond
        /// this window a replayed key would execute as a NEW request, so a
        /// stale payout must first look for a transfer it may already have
        /// created. Mirrors OrderPaymentReconciliationService's session
        /// recovery window.
        /// </summary>
        private static readonly TimeSpan IdempotencyRecoverySafetyWindow =
            TimeSpan.FromHours(23);

        /// <summary>
        /// Stripe's error code when the platform balance cannot fund a transfer
        /// (typically because the order's charge has not settled yet).
        /// </summary>
        private const string InsufficientFundsErrorCode = "balance_insufficient";

        /// <summary>
        /// Deferrals (funds still settling, wallet not usable, provider
        /// unreachable) deliberately never consume a transfer attempt, so they
        /// can repeat indefinitely. Past this age the payout is no longer
        /// "waiting normally" and is escalated so somebody looks at it.
        /// </summary>
        private static readonly TimeSpan StuckPayoutEscalationAge = TimeSpan.FromDays(3);

        private readonly ISellerPayoutRepository _sellerPayoutRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IPayoutGateway _payoutGateway;
        private readonly IPaymentGateway _paymentGateway;
        private readonly PayoutOptions _payoutOptions;
        private readonly ILogger<ProcessPendingSellerPayoutCommandHandler> _logger;

        public ProcessPendingSellerPayoutCommandHandler(
            ISellerPayoutRepository sellerPayoutRepository,
            ILibraryRepository libraryRepository,
            IPayoutGateway payoutGateway,
            IPaymentGateway paymentGateway,
            IOptions<PayoutOptions> payoutOptions,
            ILogger<ProcessPendingSellerPayoutCommandHandler> logger)
        {
            _sellerPayoutRepository = sellerPayoutRepository;
            _libraryRepository = libraryRepository;
            _payoutGateway = payoutGateway;
            _paymentGateway = paymentGateway;
            _payoutOptions = payoutOptions.Value;
            _logger = logger;
        }

        public async Task<bool> Handle(
            ProcessPendingSellerPayoutCommand request,
            CancellationToken cancellationToken)
        {
            if (request.PayoutId == Guid.Empty)
            {
                throw new DomainException("Payout id is required.");
            }

            var payout = await _sellerPayoutRepository.GetByIdAsync(
                request.PayoutId,
                cancellationToken);

            if (payout is null
                || payout.Status != SellerPayoutStatus.Pending
                || payout.NextAttemptAtUtc > DateTime.UtcNow)
            {
                return false;
            }

            if (!string.Equals(
                    payout.Currency,
                    _paymentGateway.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                payout.RecordDefinitiveRejection(
                    $"Payout currency '{payout.Currency}' is not supported by the payment gateway.",
                    _payoutOptions.MaxTransferAttempts);
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
                return false;
            }

            var transferGroup = $"order:{payout.OrderId:N}";

            var library = await _libraryRepository.GetByIdAsync(
                payout.LibraryId,
                cancellationToken);

            var destinationAccountId = library?.StripeConnectAccountId;

            if (library is null || string.IsNullOrWhiteSpace(destinationAccountId))
            {
                // The wallet is gone, but an earlier attempt may already have
                // moved the money. Reconcile before parking: adopting a
                // transfer that exists needs no wallet, and otherwise this
                // payout would sit Pending forever while the seller was in
                // fact paid.
                if (payout.LastAttemptAtUtc.HasValue
                    && await TryAdoptExistingTransferAsync(
                        payout,
                        transferGroup,
                        destinationAccountId,
                        "an earlier attempt",
                        cancellationToken))
                {
                    return true;
                }

                payout.PostponeUntilWalletConfigured(DateTime.UtcNow);
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
                return false;
            }

            if (!library.IsStripeWalletActive
                && !await TryActivateWalletAsync(library, destinationAccountId, payout, cancellationToken))
            {
                return false;
            }

            // Staleness is measured from CreationTime — a timestamp no claim
            // or retry ever refreshes — so a payout that has been looping for
            // longer than Stripe's idempotency retention ALWAYS reconciles
            // against Stripe before creating, whatever path kept it busy.
            var isStale = DateTime.UtcNow - payout.CreationTime > IdempotencyRecoverySafetyWindow;

            // Persist a short lease BEFORE contacting Stripe. The concurrency
            // token rotates with this save, so a competing replica that loaded
            // the same payout fails its own claim with a ConflictException and
            // never reaches Stripe.
            payout.ClaimForProcessing(DateTime.UtcNow);
            await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

            try
            {
                if (isStale
                    && await TryAdoptExistingTransferAsync(
                        payout,
                        transferGroup,
                        destinationAccountId,
                        "a stale payout",
                        cancellationToken))
                {
                    return true;
                }

                var sourceChargeId = await ResolveSourceChargeAsync(payout, cancellationToken);

                // The key embeds the generation counter: definitive rejections
                // and funds deferrals rotate it (a genuine re-evaluation),
                // while crash-recovery retries and indeterminate failures reuse
                // it so Stripe replays the possibly-created transfer instead of
                // paying twice. The funding mode is part of the key because a
                // replay must carry the SAME request body: a charge that could
                // not be resolved on one attempt but can be on the next would
                // otherwise reuse a generation with different parameters.
                var fundingMode = sourceChargeId is null ? "balance" : "charge";

                var transfer = await _payoutGateway.CreateTransferAsync(
                    new PayoutTransferRequest(
                        destinationAccountId,
                        payout.NetAmountMinor,
                        payout.Currency,
                        TransferGroup: transferGroup,
                        IdempotencyKey: $"seller-payout:{payout.Id:N}:{payout.IdempotencyKeyGeneration}:{fundingMode}",
                        Metadata: new Dictionary<string, string>
                        {
                            ["sellerPayoutId"] = payout.Id.ToString(),
                            ["orderId"] = payout.OrderId.ToString(),
                            ["libraryId"] = payout.LibraryId.ToString(),
                        },
                        SourceTransactionId: sourceChargeId),
                    cancellationToken);

                payout.MarkPaid(transfer.TransferId, destinationAccountId);
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Paid seller payout {PayoutId} for order {OrderId} to library {LibraryId} via transfer {TransferId}.",
                    payout.Id,
                    payout.OrderId,
                    payout.LibraryId,
                    transfer.TransferId);

                return true;
            }
            catch (PayoutConcurrentAttemptException exception)
            {
                // Another process is executing this very attempt. Its outcome
                // is the attempt's outcome — back off without recording
                // anything.
                _logger.LogDebug(
                    exception,
                    "Seller payout {PayoutId} is already being transferred by a concurrent process.",
                    payout.Id);

                return false;
            }
            catch (PayoutIdempotencyKeyReuseException exception)
            {
                // The current key generation was already used with different
                // parameters (e.g. the owner changed wallets mid-attempt).
                // Waiting never resolves this: adopt the transfer if that
                // earlier request created one, otherwise consume the attempt
                // so the key rotates and re-executes with today's wallet.
                _logger.LogWarning(
                    exception,
                    "Idempotency key for seller payout {PayoutId} was already used with different parameters; reconciling against Stripe.",
                    payout.Id);

                if (await TryAdoptExistingTransferAsync(
                        payout,
                        transferGroup,
                        destinationAccountId,
                        "an idempotency-key conflict",
                        cancellationToken))
                {
                    return true;
                }

                payout.RecordDefinitiveRejection(
                    "The transfer request conflicted with an earlier attempt. It will be retried automatically.",
                    _payoutOptions.MaxTransferAttempts);
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                return false;
            }
            catch (PayoutGatewayException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Transfer attempt failed for seller payout {PayoutId} (library {LibraryId}, definitive: {IsDefinitive}, code: {ErrorCode}).",
                    payout.Id,
                    payout.LibraryId,
                    exception.IsDefinitiveRejection,
                    exception.ErrorCode);

                await RecordTransferFailureAsync(
                    payout,
                    library,
                    destinationAccountId,
                    exception,
                    cancellationToken);

                return false;
            }
        }

        /// <summary>
        /// Adopts a transfer that already exists at the provider for this
        /// payout, so money that has moved is never sent twice and never lost.
        /// </summary>
        private async Task<bool> TryAdoptExistingTransferAsync(
            SellerPayoutAggregate payout,
            string transferGroup,
            string? fallbackDestinationAccountId,
            string context,
            CancellationToken cancellationToken)
        {
            PayoutExistingTransfer? existingTransfer;

            try
            {
                existingTransfer = await _payoutGateway.FindTransferForPayoutAsync(
                    transferGroup,
                    payout.Id,
                    cancellationToken);
            }
            catch (PayoutGatewayException exception)
            {
                // Reconciliation is best-effort: the caller continues on its
                // normal path and the payout retries later.
                _logger.LogWarning(
                    exception,
                    "Could not reconcile seller payout {PayoutId} against Stripe ({Context}).",
                    payout.Id,
                    context);

                return false;
            }

            if (existingTransfer is null)
            {
                return false;
            }

            var destinationAccountId = Coalesce(
                existingTransfer.DestinationAccountId,
                fallbackDestinationAccountId,
                payout.DestinationStripeAccountId);

            if (destinationAccountId is null)
            {
                // Practically unreachable (the provider always names the
                // destination). Never throw here: money has moved, so surface it
                // loudly and let the payout retry instead of crashing recovery.
                _logger.LogError(
                    "Transfer {TransferId} for seller payout {PayoutId} has no destination account; it could not be recorded automatically.",
                    existingTransfer.TransferId,
                    payout.Id);

                return false;
            }

            payout.MarkPaid(existingTransfer.TransferId, destinationAccountId);

            await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Adopted existing transfer {TransferId} for seller payout {PayoutId} after {Context}.",
                existingTransfer.TransferId,
                payout.Id,
                context);

            return true;
        }

        /// <summary>
        /// Raises a payout that has been deferring for longer than sellers
        /// should ever wait. Deferrals never reach the Failed state by design,
        /// so this is the only signal that one is stuck.
        /// </summary>
        private void EscalateIfStuck(SellerPayoutAggregate payout, string reason)
        {
            if (DateTime.UtcNow - payout.CreationTime <= StuckPayoutEscalationAge)
            {
                return;
            }

            _logger.LogError(
                "Seller payout {PayoutId} for order {OrderId} (library {LibraryId}) has been waiting since {CreatedAtUtc:o} because {Reason}; it needs manual review.",
                payout.Id,
                payout.OrderId,
                payout.LibraryId,
                payout.CreationTime,
                reason);
        }

        private static string? Coalesce(params string?[] candidates)
        {
            return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        }

        /// <summary>
        /// Asks the provider whether an attached-but-inactive wallet can now
        /// receive transfers; activates it (the owner may have finished
        /// onboarding without returning to the app) or parks the payout.
        /// </summary>
        private async Task<bool> TryActivateWalletAsync(
            LibraryAggregate library,
            string destinationAccountId,
            SellerPayoutAggregate payout,
            CancellationToken cancellationToken)
        {
            PayoutConnectedAccountStatus? accountStatus;

            try
            {
                accountStatus = await _payoutGateway.GetConnectedAccountAsync(
                    destinationAccountId,
                    cancellationToken);
            }
            catch (PayoutGatewayException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not verify Stripe wallet {AccountId} of library {LibraryId}; payout {PayoutId} postponed.",
                    destinationAccountId,
                    library.Id,
                    payout.Id);
                accountStatus = null;
            }

            if (accountStatus is null || !accountStatus.CanReceiveTransfers)
            {
                payout.PostponeUntilWalletConfigured(
                    DateTime.UtcNow,
                    "The library's Stripe wallet cannot receive transfers yet.");
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
                return false;
            }

            library.MarkStripeWalletActive(DateTime.UtcNow);
            await _libraryRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Stripe wallet {AccountId} of library {LibraryId} became active; proceeding with payout {PayoutId}.",
                destinationAccountId,
                library.Id,
                payout.Id);

            return true;
        }

        /// <summary>
        /// Resolves (and caches) the charge that funds this payout, so the
        /// transfer draws on the order's own payment instead of the platform's
        /// available balance.
        /// </summary>
        private async Task<string?> ResolveSourceChargeAsync(
            SellerPayoutAggregate payout,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(payout.SourceChargeId))
            {
                return payout.SourceChargeId;
            }

            if (string.IsNullOrWhiteSpace(payout.SourcePaymentIntentId))
            {
                return null;
            }

            string? chargeId;

            try
            {
                chargeId = await _payoutGateway.ResolveChargeIdAsync(
                    payout.SourcePaymentIntentId,
                    cancellationToken);
            }
            catch (PayoutGatewayException exception)
            {
                // Best-effort funding hint: fall back to a balance-funded
                // transfer rather than failing (and charging an attempt for)
                // the payout itself.
                _logger.LogWarning(
                    exception,
                    "Could not resolve the charge of payment {PaymentIntentId} for seller payout {PayoutId}; the transfer will draw on the platform balance.",
                    payout.SourcePaymentIntentId,
                    payout.Id);

                return null;
            }

            if (string.IsNullOrWhiteSpace(chargeId))
            {
                _logger.LogWarning(
                    "Payment {PaymentIntentId} of seller payout {PayoutId} has no charge; the transfer will draw on the platform balance.",
                    payout.SourcePaymentIntentId,
                    payout.Id);

                return null;
            }

            payout.AttachSourceCharge(chargeId);
            await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

            return chargeId;
        }

        /// <summary>
        /// Classifies a failed transfer: funds that have not settled and wallets
        /// the provider has disabled are deferrals, not rejections, so they
        /// never exhaust the retry budget and never mark a payout Failed.
        /// </summary>
        private async Task RecordTransferFailureAsync(
            SellerPayoutAggregate payout,
            LibraryAggregate library,
            string destinationAccountId,
            PayoutGatewayException exception,
            CancellationToken cancellationToken)
        {
            if (string.Equals(
                    exception.ErrorCode,
                    InsufficientFundsErrorCode,
                    StringComparison.Ordinal))
            {
                payout.RecordFundsUnavailable(
                    "The payment is still settling. The transfer will be retried automatically.");
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
                EscalateIfStuck(payout, "the funds backing it have not settled");
                return;
            }

            // Owner-facing FailureReason stays neutral: raw provider text
            // describes the PLATFORM account's state and is kept in logs only.
            if (!exception.IsDefinitiveRejection)
            {
                payout.RecordIndeterminateFailure(
                    "The transfer could not be completed. It will be retried automatically.");
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
                EscalateIfStuck(payout, "the payment provider keeps failing the request");
                return;
            }

            // A definitive rejection may mean the destination wallet itself is
            // no longer able to receive transfers (the provider disabled the
            // account after onboarding). Re-check before spending an attempt:
            // a wallet problem is the owner's to fix, not a payout failure.
            if (await IsWalletUnusableAsync(destinationAccountId, cancellationToken))
            {
                library.DeactivateStripeWallet();
                await _libraryRepository.SaveChangesAsync();

                payout.PostponeUntilWalletConfigured(
                    DateTime.UtcNow,
                    "The library's Stripe wallet cannot receive transfers yet.");
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Stripe wallet {AccountId} of library {LibraryId} can no longer receive transfers; it was deactivated and payout {PayoutId} is waiting.",
                    destinationAccountId,
                    library.Id,
                    payout.Id);

                EscalateIfStuck(payout, "its library has no wallet that can receive transfers");
                return;
            }

            payout.RecordDefinitiveRejection(
                "The payment provider declined the transfer. It will be retried automatically.",
                _payoutOptions.MaxTransferAttempts);
            await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

            if (payout.Status == SellerPayoutStatus.Failed)
            {
                _logger.LogError(
                    "Seller payout {PayoutId} exhausted its {MaxAttempts} transfer attempts and requires manual review.",
                    payout.Id,
                    _payoutOptions.MaxTransferAttempts);
            }
        }

        private async Task<bool> IsWalletUnusableAsync(
            string destinationAccountId,
            CancellationToken cancellationToken)
        {
            try
            {
                var accountStatus = await _payoutGateway.GetConnectedAccountAsync(
                    destinationAccountId,
                    cancellationToken);

                return accountStatus is null || !accountStatus.CanReceiveTransfers;
            }
            catch (PayoutGatewayException)
            {
                // Cannot tell — treat the original rejection at face value.
                return false;
            }
        }
    }
}

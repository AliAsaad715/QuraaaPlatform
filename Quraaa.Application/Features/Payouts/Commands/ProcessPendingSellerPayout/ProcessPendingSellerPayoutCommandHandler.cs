using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Interfaces;
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

            var library = await _libraryRepository.GetByIdAsync(
                payout.LibraryId,
                cancellationToken);

            var destinationAccountId = library?.StripeConnectAccountId;

            if (string.IsNullOrWhiteSpace(destinationAccountId))
            {
                payout.PostponeUntilWalletConfigured(DateTime.UtcNow);
                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);
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

            var transferGroup = $"order:{payout.OrderId:N}";

            try
            {
                if (isStale)
                {
                    // The idempotency key may have expired at Stripe, so a
                    // replay could create a SECOND transfer. Adopt an existing
                    // transfer for this payout if one exists.
                    var existingTransfer = await _payoutGateway.FindTransferForPayoutAsync(
                        transferGroup,
                        payout.Id,
                        cancellationToken);

                    if (existingTransfer is not null)
                    {
                        payout.MarkPaid(
                            existingTransfer.TransferId,
                            string.IsNullOrWhiteSpace(existingTransfer.DestinationAccountId)
                                ? destinationAccountId
                                : existingTransfer.DestinationAccountId);
                        await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Adopted existing transfer {TransferId} for stale seller payout {PayoutId}.",
                            existingTransfer.TransferId,
                            payout.Id);

                        return true;
                    }
                }

                // The key embeds AttemptCount as a generation: definitive
                // rejections consume an attempt and therefore rotate the key
                // (a genuine re-evaluation), while crash-recovery retries and
                // indeterminate failures reuse the same generation so Stripe
                // replays the possibly-created transfer instead of paying
                // twice.
                var transfer = await _payoutGateway.CreateTransferAsync(
                    new PayoutTransferRequest(
                        destinationAccountId,
                        payout.NetAmountMinor,
                        payout.Currency,
                        TransferGroup: transferGroup,
                        IdempotencyKey: $"seller-payout:{payout.Id:N}:{payout.AttemptCount}",
                        Metadata: new Dictionary<string, string>
                        {
                            ["sellerPayoutId"] = payout.Id.ToString(),
                            ["orderId"] = payout.OrderId.ToString(),
                            ["libraryId"] = payout.LibraryId.ToString(),
                        }),
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

                var conflictingTransfer = await _payoutGateway.FindTransferForPayoutAsync(
                    transferGroup,
                    payout.Id,
                    cancellationToken);

                if (conflictingTransfer is not null)
                {
                    payout.MarkPaid(
                        conflictingTransfer.TransferId,
                        string.IsNullOrWhiteSpace(conflictingTransfer.DestinationAccountId)
                            ? destinationAccountId
                            : conflictingTransfer.DestinationAccountId);
                    await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Adopted existing transfer {TransferId} for seller payout {PayoutId} after an idempotency-key conflict.",
                        conflictingTransfer.TransferId,
                        payout.Id);

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

                // Owner-facing FailureReason stays neutral: raw provider text
                // describes the PLATFORM account's state and is kept in logs
                // only.
                if (exception.IsDefinitiveRejection)
                {
                    payout.RecordDefinitiveRejection(
                        "The payment provider declined the transfer. It will be retried automatically.",
                        _payoutOptions.MaxTransferAttempts);
                }
                else
                {
                    payout.RecordIndeterminateFailure(
                        "The transfer could not be completed. It will be retried automatically.");
                }

                await _sellerPayoutRepository.SaveChangesAsync(cancellationToken);

                if (payout.Status == SellerPayoutStatus.Failed)
                {
                    _logger.LogError(
                        "Seller payout {PayoutId} exhausted its {MaxAttempts} transfer attempts and requires manual review.",
                        payout.Id,
                        _payoutOptions.MaxTransferAttempts);
                }

                return false;
            }
        }
    }
}

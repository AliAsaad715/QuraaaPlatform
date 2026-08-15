using Quraaa.Domain.Payouts.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Payouts
{
    /// <summary>
    /// Transactional-outbox record for a library seller's share of a paid order.
    /// Rows are staged in the same database transaction that marks the order
    /// paid; a background processor later moves the money through the payment
    /// provider and records the outcome here.
    /// </summary>
    public class SellerPayoutAggregate : AggregateRoot
    {
        public const int MaxFailureReasonLength = 1000;

        private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(6);
        private static readonly TimeSpan WalletMissingRetryDelay = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan IndeterminateRetryDelay = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ProcessingLeaseDuration = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How long <see cref="ClaimForProcessing"/> reserves a payout for the
        /// worker that is contacting the provider. Persistence uses it to leave
        /// in-flight payouts alone.
        /// </summary>
        public static TimeSpan ProcessingLease => ProcessingLeaseDuration;

        /// <summary>
        /// How long to wait when the provider says the funds backing this
        /// payout are not available yet. Card charges typically settle in a few
        /// business days, so retrying sooner only wastes provider calls.
        /// </summary>
        private static readonly TimeSpan FundsUnavailableRetryDelay = TimeSpan.FromHours(6);

        /// <summary>Highest exponent used by the retry backoff (6 h cap).</summary>
        private const int MaxRetryExponent = 7;

        public Guid OrderId { get; private set; }
        public Guid LibraryId { get; private set; }
        public string Currency { get; private set; } = null!;
        public long GrossAmountMinor { get; private set; }

        /// <summary>
        /// The library's profit-share percentage at the moment the order was
        /// paid (snapshotted from the library so later admin changes do not
        /// rewrite history).
        /// </summary>
        public decimal ProfitSharePercent { get; private set; }

        /// <summary>The platform's remainder: gross minus the library's net.</summary>
        public long PlatformFeeMinor { get; private set; }

        /// <summary>The amount transferred to the library's wallet.</summary>
        public long NetAmountMinor { get; private set; }
        public SellerPayoutStatus Status { get; private set; }
        public int AttemptCount { get; private set; }
        public DateTime NextAttemptAtUtc { get; private set; }
        public DateTime? LastAttemptAtUtc { get; private set; }
        public DateTime? PaidAtUtc { get; private set; }
        public string? StripeTransferId { get; private set; }
        public string? DestinationStripeAccountId { get; private set; }
        public string? FailureReason { get; private set; }

        /// <summary>
        /// The provider payment that funds this payout (the order's payment
        /// intent). The transfer is drawn from this specific payment, so it can
        /// be created before the charge settles into the platform balance.
        /// </summary>
        public string? SourcePaymentIntentId { get; private set; }

        /// <summary>
        /// The charge resolved from <see cref="SourcePaymentIntentId"/>, cached
        /// after the first lookup so later attempts need no extra provider call.
        /// </summary>
        public string? SourceChargeId { get; private set; }

        /// <summary>
        /// Generation of the provider idempotency key. Rotated whenever an
        /// attempt is definitively rejected or deferred for funds, so the next
        /// request is genuinely re-evaluated instead of replaying a cached
        /// error; NOT rotated for indeterminate outcomes, whose retry must
        /// replay a possibly-created transfer.
        /// </summary>
        public int IdempotencyKeyGeneration { get; private set; }

        private SellerPayoutAggregate() { }

        public static SellerPayoutAggregate Create(
            Guid orderId,
            Guid libraryId,
            string currency,
            long grossAmountMinor,
            decimal profitSharePercent,
            string? sourcePaymentIntentId = null)
        {
            if (orderId == Guid.Empty)
            {
                throw new DomainException("Seller payout order id is required.");
            }

            if (libraryId == Guid.Empty)
            {
                throw new DomainException("Seller payout library id is required.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new DomainException("Seller payout currency is required.");
            }

            if (grossAmountMinor <= 0)
            {
                throw new DomainException("Seller payout gross amount must be positive.");
            }

            if (profitSharePercent is < 0m or > 100m)
            {
                throw new DomainException(
                    "Seller payout profit share percent must be between 0 and 100.");
            }

            var netAmountMinor = CalculateNetAmountMinor(
                grossAmountMinor,
                profitSharePercent);
            var platformFeeMinor = grossAmountMinor - netAmountMinor;

            var utcNow = DateTime.UtcNow;

            return new SellerPayoutAggregate
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                LibraryId = libraryId,
                Currency = currency.Trim().ToLowerInvariant(),
                GrossAmountMinor = grossAmountMinor,
                ProfitSharePercent = profitSharePercent,
                PlatformFeeMinor = platformFeeMinor,
                NetAmountMinor = netAmountMinor,
                // A share that rounds to zero minor units has nothing to
                // transfer; keep the row for the owner's history and audit.
                Status = netAmountMinor > 0
                    ? SellerPayoutStatus.Pending
                    : SellerPayoutStatus.NoAmountDue,
                AttemptCount = 0,
                IdempotencyKeyGeneration = 0,
                NextAttemptAtUtc = utcNow,
                SourcePaymentIntentId = string.IsNullOrWhiteSpace(sourcePaymentIntentId)
                    ? null
                    : sourcePaymentIntentId.Trim(),
            };
        }

        /// <summary>
        /// The library's share of the gross amount, rounded half away from
        /// zero to whole minor units so splits are deterministic and
        /// net + fee always equals gross.
        /// </summary>
        public static long CalculateNetAmountMinor(
            long grossAmountMinor,
            decimal profitSharePercent)
        {
            var netAmountMinor = (long)decimal.Round(
                grossAmountMinor * profitSharePercent / 100m,
                0,
                MidpointRounding.AwayFromZero);

            return Math.Clamp(netAmountMinor, 0, grossAmountMinor);
        }

        /// <summary>
        /// Caches the charge resolved from <see cref="SourcePaymentIntentId"/>
        /// so later attempts skip the provider lookup.
        /// </summary>
        public void AttachSourceCharge(string sourceChargeId)
        {
            if (string.IsNullOrWhiteSpace(sourceChargeId))
            {
                throw new DomainException("A source charge id is required.");
            }

            var normalized = sourceChargeId.Trim();

            if (string.Equals(SourceChargeId, normalized, StringComparison.Ordinal))
            {
                return;
            }

            SourceChargeId = normalized;
            UpdateModificationTime();
        }

        public void MarkPaid(string stripeTransferId, string destinationStripeAccountId)
        {
            if (string.IsNullOrWhiteSpace(stripeTransferId))
            {
                throw new DomainException("A Stripe transfer id is required to mark a payout paid.");
            }

            if (string.IsNullOrWhiteSpace(destinationStripeAccountId))
            {
                throw new DomainException(
                    "A destination Stripe account id is required to mark a payout paid.");
            }

            if (Status == SellerPayoutStatus.Paid)
            {
                if (!string.Equals(
                        StripeTransferId,
                        stripeTransferId.Trim(),
                        StringComparison.Ordinal))
                {
                    throw new ConflictException(
                        "A different Stripe transfer already paid this payout.");
                }

                return;
            }

            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can be marked paid.");
            }

            var utcNow = DateTime.UtcNow;

            Status = SellerPayoutStatus.Paid;
            StripeTransferId = stripeTransferId.Trim();
            DestinationStripeAccountId = destinationStripeAccountId.Trim();
            PaidAtUtc = utcNow;
            LastAttemptAtUtc = utcNow;
            FailureReason = null;
            UpdateModificationTime();
        }

        /// <summary>
        /// Leases the payout to the calling processor for a short window before
        /// the provider is contacted. Persisting the lease first (the
        /// concurrency token rotates with it) makes a competing replica's save
        /// fail, so at most one process talks to the provider per attempt.
        /// </summary>
        public void ClaimForProcessing(DateTime utcNow)
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can be claimed for processing.");
            }

            LastAttemptAtUtc = utcNow;
            NextAttemptAtUtc = utcNow.Add(ProcessingLeaseDuration);
            UpdateModificationTime();
        }

        /// <summary>
        /// Records a failure where the provider definitively rejected the
        /// request, i.e. no transfer was created. Consumes a transfer attempt
        /// AND rotates <see cref="IdempotencyKeyGeneration"/>, so the next
        /// attempt is genuinely re-evaluated instead of replaying the cached
        /// rejection.
        /// </summary>
        public void RecordDefinitiveRejection(string failureReason, int maxAttempts)
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can record a failure.");
            }

            if (maxAttempts < 1)
            {
                throw new DomainException("Payout max attempts must be at least 1.");
            }

            var utcNow = DateTime.UtcNow;

            AttemptCount++;
            IdempotencyKeyGeneration++;
            LastAttemptAtUtc = utcNow;
            FailureReason = Truncate(failureReason);

            if (AttemptCount >= maxAttempts)
            {
                Status = SellerPayoutStatus.Failed;
            }
            else
            {
                NextAttemptAtUtc = utcNow.Add(CalculateRetryDelay(AttemptCount));
            }

            UpdateModificationTime();
        }

        /// <summary>
        /// Records a failure whose outcome is unknown (timeout, transport
        /// error, provider 5xx): the transfer may or may not exist. Does NOT
        /// consume an attempt and keeps the idempotency key stable, so the
        /// retry safely replays a possibly-created transfer instead of paying
        /// twice — and the payout can never be marked Failed while money may
        /// already have moved.
        /// </summary>
        public void RecordIndeterminateFailure(string failureReason)
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can record a failure.");
            }

            var utcNow = DateTime.UtcNow;

            LastAttemptAtUtc = utcNow;
            FailureReason = Truncate(failureReason);
            NextAttemptAtUtc = utcNow.Add(IndeterminateRetryDelay);
            UpdateModificationTime();
        }

        /// <summary>
        /// Defers the payout because the provider does not have the backing
        /// funds available yet. Rotates the idempotency key (the cached error
        /// must not be replayed) but does NOT consume a transfer attempt, so
        /// waiting for settlement can never exhaust the retry budget.
        /// </summary>
        public void RecordFundsUnavailable(string failureReason)
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can record a failure.");
            }

            var utcNow = DateTime.UtcNow;

            IdempotencyKeyGeneration++;
            LastAttemptAtUtc = utcNow;
            FailureReason = Truncate(failureReason);
            NextAttemptAtUtc = utcNow.Add(FundsUnavailableRetryDelay);
            UpdateModificationTime();
        }

        /// <summary>
        /// Defers the payout until the library configures a Stripe wallet.
        /// Intentionally does not consume a failure attempt: waiting for
        /// onboarding is not an error.
        /// </summary>
        public void PostponeUntilWalletConfigured(
            DateTime utcNow,
            string reason = "The library has no Stripe wallet configured.")
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can be postponed.");
            }

            FailureReason = Truncate(reason);
            NextAttemptAtUtc = utcNow.Add(WalletMissingRetryDelay);
            UpdateModificationTime();
        }

        private static TimeSpan CalculateRetryDelay(int attemptCount)
        {
            // 5m, 10m, 20m, 40m, 80m, 160m, 320m, then the 6 h cap.
            var exponent = Math.Clamp(attemptCount - 1, 0, MaxRetryExponent);
            var delay = TimeSpan.FromTicks(BaseRetryDelay.Ticks << exponent);

            return delay > MaxRetryDelay ? MaxRetryDelay : delay;
        }

        private static string Truncate(string value)
        {
            var trimmed = string.IsNullOrWhiteSpace(value)
                ? "The payout transfer failed."
                : value.Trim();

            return trimmed.Length <= MaxFailureReasonLength
                ? trimmed
                : trimmed[..MaxFailureReasonLength];
        }
    }
}

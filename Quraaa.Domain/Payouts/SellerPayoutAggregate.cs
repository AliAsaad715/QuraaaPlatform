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

        public Guid OrderId { get; private set; }
        public Guid LibraryId { get; private set; }
        public string Currency { get; private set; } = null!;
        public long GrossAmountMinor { get; private set; }
        public decimal CommissionPercent { get; private set; }
        public long PlatformFeeMinor { get; private set; }
        public long NetAmountMinor { get; private set; }
        public SellerPayoutStatus Status { get; private set; }
        public int AttemptCount { get; private set; }
        public DateTime NextAttemptAtUtc { get; private set; }
        public DateTime? LastAttemptAtUtc { get; private set; }
        public DateTime? PaidAtUtc { get; private set; }
        public string? StripeTransferId { get; private set; }
        public string? DestinationStripeAccountId { get; private set; }
        public string? FailureReason { get; private set; }

        private SellerPayoutAggregate() { }

        public static SellerPayoutAggregate Create(
            Guid orderId,
            Guid libraryId,
            string currency,
            long grossAmountMinor,
            decimal commissionPercent)
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

            if (commissionPercent is < 0m or > 100m)
            {
                throw new DomainException(
                    "Seller payout commission percent must be between 0 and 100.");
            }

            var platformFeeMinor = CalculatePlatformFeeMinor(
                grossAmountMinor,
                commissionPercent);
            var netAmountMinor = grossAmountMinor - platformFeeMinor;

            if (netAmountMinor <= 0)
            {
                throw new DomainException("Seller payout net amount must be positive.");
            }

            return new SellerPayoutAggregate
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                LibraryId = libraryId,
                Currency = currency.Trim().ToLowerInvariant(),
                GrossAmountMinor = grossAmountMinor,
                CommissionPercent = commissionPercent,
                PlatformFeeMinor = platformFeeMinor,
                NetAmountMinor = netAmountMinor,
                Status = SellerPayoutStatus.Pending,
                AttemptCount = 0,
                NextAttemptAtUtc = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// The platform's cut of the gross amount, rounded away from zero so
        /// splits are deterministic and fee + net always equals gross.
        /// </summary>
        public static long CalculatePlatformFeeMinor(
            long grossAmountMinor,
            decimal commissionPercent)
        {
            return (long)decimal.Round(
                grossAmountMinor * commissionPercent / 100m,
                0,
                MidpointRounding.AwayFromZero);
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
        /// request, i.e. no transfer was created. Consumes a transfer attempt,
        /// which also rotates the provider idempotency key
        /// (<see cref="AttemptCount"/> is the key generation), so the next
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
        /// Defers the payout until the library configures a Stripe wallet.
        /// Intentionally does not consume a failure attempt: waiting for
        /// onboarding is not an error.
        /// </summary>
        public void PostponeUntilWalletConfigured(DateTime utcNow)
        {
            if (Status != SellerPayoutStatus.Pending)
            {
                throw new DomainException("Only pending payouts can be postponed.");
            }

            FailureReason = "The library has no Stripe wallet configured.";
            NextAttemptAtUtc = utcNow.Add(WalletMissingRetryDelay);
            UpdateModificationTime();
        }

        private static TimeSpan CalculateRetryDelay(int attemptCount)
        {
            var exponent = Math.Clamp(attemptCount - 1, 0, 30);
            var delayTicks = BaseRetryDelay.Ticks * (1L << exponent);

            return delayTicks < 0 || delayTicks > MaxRetryDelay.Ticks
                ? MaxRetryDelay
                : TimeSpan.FromTicks(delayTicks);
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

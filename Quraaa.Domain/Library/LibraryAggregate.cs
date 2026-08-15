using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Library
{
    public class LibraryAggregate : AggregateRoot
    {
        public string LibraryName { get; private set; } = null!;
        public string Location { get; private set; } = null!;
        public string LibraryImage { get; private set; } = null!;
        public string HeaderImage { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public LibraryApprovalStatus ApprovalStatus { get; private set; }
        public DateTime? EmailVerifiedAtUtc { get; private set; }
        /// <summary>
        /// Hash of the library dashboard's own password. Separate from the
        /// owner's personal account password so the two credentials can never
        /// be the same secret.
        /// </summary>
        public string? PasswordHash { get; private set; }

        /// <summary>
        /// The Stripe Connect account (wallet) profit shares are transferred
        /// to. Created through Stripe-hosted onboarding started by the owner,
        /// or attached by id.
        /// </summary>
        public string? StripeConnectAccountId { get; private set; }

        /// <summary>
        /// When the wallet was last confirmed able to receive transfers
        /// (Stripe onboarding finished). Null while onboarding is incomplete.
        /// </summary>
        public DateTime? StripeWalletActivatedAtUtc { get; private set; }

        /// <summary>
        /// The percentage of this library's gross sales that is paid out to the
        /// library owner; the platform keeps the remainder. Set by
        /// administrators; new libraries start at
        /// <see cref="DefaultProfitSharePercent"/>.
        /// </summary>
        public decimal ProfitSharePercent { get; private set; }

        public Guid ConcurrencyStamp { get; private set; }

        /// <summary>
        /// The profit-share percentage every library starts with until an
        /// administrator changes it.
        /// </summary>
        public const decimal DefaultProfitSharePercent = 0.005m;

        /// <summary>
        /// Maximum number of decimal places a profit-share percentage may
        /// carry; matches the precision persisted on libraries and payouts.
        /// </summary>
        public const int ProfitSharePercentScale = 4;

        private LibraryAggregate() { }

        public LibraryAggregate(
            Guid id,
            string libraryName,
            string location,
            string libraryImage,
            string headerImage,
            string email,
            Guid userId,
            string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainException("A library password is required.");
            }

            Id = id;
            PasswordHash = passwordHash;
            LibraryName = libraryName;
            Location = location;
            LibraryImage = libraryImage;
            HeaderImage = headerImage;
            Email = email;
            UserId = userId;
            ApprovalStatus = LibraryApprovalStatus.AwaitingEmailVerification;
            ProfitSharePercent = DefaultProfitSharePercent;
            ConcurrencyStamp = Guid.NewGuid();
        }

        public void VerifyEmail(DateTime utcNow)
        {
            if (EmailVerifiedAtUtc.HasValue)
            {
                return;
            }

            if (ApprovalStatus is not (
                LibraryApprovalStatus.AwaitingEmailVerification or
                LibraryApprovalStatus.Pending))
            {
                throw new DomainException("Only libraries awaiting email verification can verify their email.");
            }

            EmailVerifiedAtUtc ??= NormalizeUtc(utcNow);
            ApprovalStatus = LibraryApprovalStatus.Pending;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateModificationTime();
        }

        public void Approve(Guid modifiedBy)
        {
            if (ApprovalStatus != LibraryApprovalStatus.Pending || !EmailVerifiedAtUtc.HasValue)
            {
                throw new DomainException("Only email-verified pending libraries can be approved.");
            }

            ApprovalStatus = LibraryApprovalStatus.Approved;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        public void Reject(Guid modifiedBy)
        {
            if (ApprovalStatus != LibraryApprovalStatus.Pending || !EmailVerifiedAtUtc.HasValue)
            {
                throw new DomainException("Only email-verified pending libraries can be rejected.");
            }

            ApprovalStatus = LibraryApprovalStatus.Rejected;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        public bool IsStripeWalletActive =>
            StripeConnectAccountId is not null && StripeWalletActivatedAtUtc.HasValue;

        public LibraryWalletStatus WalletStatus =>
            StripeConnectAccountId is null
                ? LibraryWalletStatus.NotConnected
                : StripeWalletActivatedAtUtc.HasValue
                    ? LibraryWalletStatus.Active
                    : LibraryWalletStatus.OnboardingIncomplete;

        /// <summary>
        /// Replaces the library dashboard password. The caller hashes it; the
        /// aggregate never sees the plain text.
        /// </summary>
        public void SetPasswordHash(string passwordHash, Guid modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainException("A library password is required.");
            }

            PasswordHash = passwordHash;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        /// <summary>
        /// Attaches a Stripe connected account as this library's wallet.
        /// Allowed at any point of the library's life except after rejection,
        /// so owners can connect Stripe during registration. Pass
        /// <paramref name="activatedAtUtc"/> when the account is already known
        /// to receive transfers; leave it null while Stripe onboarding is still
        /// in progress.
        /// </summary>
        public void ConnectStripeWallet(
            string stripeConnectAccountId,
            DateTime? activatedAtUtc,
            Guid modifiedBy)
        {
            if (ApprovalStatus == LibraryApprovalStatus.Rejected)
            {
                throw new DomainException(
                    "A rejected library cannot configure a Stripe wallet.");
            }

            if (string.IsNullOrWhiteSpace(stripeConnectAccountId))
            {
                throw new DomainException("A Stripe account id is required.");
            }

            var normalizedAccountId = stripeConnectAccountId.Trim();

            if (!normalizedAccountId.StartsWith("acct_", StringComparison.Ordinal))
            {
                throw new DomainException(
                    "The Stripe wallet must be a connected account id starting with acct_.");
            }

            var normalizedActivatedAt = activatedAtUtc.HasValue
                ? NormalizeUtc(activatedAtUtc.Value)
                : (DateTime?)null;

            if (string.Equals(
                    StripeConnectAccountId,
                    normalizedAccountId,
                    StringComparison.Ordinal)
                && StripeWalletActivatedAtUtc.HasValue == normalizedActivatedAt.HasValue)
            {
                return;
            }

            StripeConnectAccountId = normalizedAccountId;
            StripeWalletActivatedAtUtc = normalizedActivatedAt;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        /// <summary>
        /// Records that the attached wallet finished Stripe onboarding and can
        /// receive transfers. Idempotent.
        /// </summary>
        public void MarkStripeWalletActive(DateTime utcNow)
        {
            if (StripeConnectAccountId is null)
            {
                throw new DomainException(
                    "The library has no Stripe wallet to activate.");
            }

            if (StripeWalletActivatedAtUtc.HasValue)
            {
                return;
            }

            StripeWalletActivatedAtUtc = NormalizeUtc(utcNow);
            ConcurrencyStamp = Guid.NewGuid();
            UpdateModificationTime();
        }

        /// <summary>
        /// Records that the attached wallet can no longer receive transfers
        /// (the provider disabled or restricted the account). Keeps the account
        /// attached so the owner can finish the provider requirements and have
        /// it re-activated. Idempotent.
        /// </summary>
        public void DeactivateStripeWallet()
        {
            if (StripeConnectAccountId is null || !StripeWalletActivatedAtUtc.HasValue)
            {
                return;
            }

            StripeWalletActivatedAtUtc = null;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateModificationTime();
        }

        public void RemoveStripeWallet(Guid modifiedBy)
        {
            if (StripeConnectAccountId is null)
            {
                return;
            }

            StripeConnectAccountId = null;
            StripeWalletActivatedAtUtc = null;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        /// <summary>
        /// Sets the percentage of gross sales paid out to this library's owner.
        /// Applies to orders paid from now on; payouts already staged keep the
        /// percentage they were computed with.
        /// </summary>
        public void SetProfitSharePercent(decimal profitSharePercent, Guid modifiedBy)
        {
            if (profitSharePercent is < 0m or > 100m)
            {
                throw new DomainException(
                    "The profit share percentage must be between 0 and 100.");
            }

            if (decimal.Round(profitSharePercent, ProfitSharePercentScale) != profitSharePercent)
            {
                throw new DomainException(
                    $"The profit share percentage may have at most {ProfitSharePercentScale} decimal places.");
            }

            if (ProfitSharePercent == profitSharePercent)
            {
                return;
            }

            ProfitSharePercent = profitSharePercent;
            ConcurrencyStamp = Guid.NewGuid();
            UpdateAudit(modifiedBy);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}

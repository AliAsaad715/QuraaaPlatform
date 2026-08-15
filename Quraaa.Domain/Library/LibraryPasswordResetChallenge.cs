using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Library
{
    /// <summary>
    /// The email one-time code that lets a library owner who lost the dashboard
    /// password set a new one. Mirrors the registration email challenge, but
    /// lives on its own row so a consumed registration verification stays
    /// consumed, and so one library can reset its password many times.
    /// </summary>
    public sealed class LibraryPasswordResetChallenge : AggregateRoot
    {
        public Guid LibraryId { get; private set; }
        public string? CodeHash { get; private set; }
        public Guid Generation { get; private set; }
        public DateTime ExpiresAtUtc { get; private set; }
        public DateTime ResendAvailableAtUtc { get; private set; }
        public int FailedAttemptCount { get; private set; }
        public DateTime? LockedUntilUtc { get; private set; }
        public DateTime? ConsumedAtUtc { get; private set; }
        public DateTime? SendWindowStartedAtUtc { get; private set; }
        public int SendCount { get; private set; }
        public Guid ConcurrencyStamp { get; private set; }

        private LibraryPasswordResetChallenge() { }

        public LibraryPasswordResetChallenge(
            Guid id,
            Guid libraryId,
            string codeHash,
            Guid generation,
            DateTime issuedAtUtc,
            DateTime expiresAtUtc,
            DateTime resendAvailableAtUtc)
        {
            if (id == Guid.Empty)
            {
                throw new DomainException("Library password reset challenge id is required.");
            }

            if (libraryId == Guid.Empty)
            {
                throw new DomainException("Library id is required for a password reset.");
            }

            var normalizedIssuedAt = NormalizeUtc(issuedAtUtc);
            ValidateIssue(codeHash, generation, normalizedIssuedAt, expiresAtUtc, resendAvailableAtUtc);

            Id = id;
            LibraryId = libraryId;
            CodeHash = codeHash.Trim();
            Generation = generation;
            ExpiresAtUtc = NormalizeUtc(expiresAtUtc);
            ResendAvailableAtUtc = NormalizeUtc(resendAvailableAtUtc);
            SendWindowStartedAtUtc = normalizedIssuedAt;
            SendCount = 1;
            ConcurrencyStamp = Guid.NewGuid();
        }

        /// <summary>
        /// Begins a fresh reset: a new generation (so any code already in an
        /// inbox stops working), a clean attempt counter, and no consumed
        /// marker. Send quota and cooldown still apply, and a locked-out
        /// challenge cannot be restarted — otherwise requesting a new code
        /// would clear the lockout.
        /// </summary>
        public void StartNewCycle(
            string codeHash,
            Guid generation,
            DateTime utcNow,
            DateTime expiresAtUtc,
            DateTime resendAvailableAtUtc,
            int maxSends,
            TimeSpan sendWindow)
        {
            var normalizedNow = NormalizeUtc(utcNow);

            if (!CanStartCycleAt(normalizedNow, maxSends, sendWindow))
            {
                throw new DomainException("A library password reset code cannot be sent yet.");
            }

            if (generation == Guid.Empty || generation == Generation)
            {
                throw new DomainException("A new password reset cycle requires a new generation.");
            }

            ValidateIssue(codeHash, generation, normalizedNow, expiresAtUtc, resendAvailableAtUtc);

            if (!SendWindowStartedAtUtc.HasValue ||
                normalizedNow >= SendWindowStartedAtUtc.Value.Add(sendWindow))
            {
                SendWindowStartedAtUtc = normalizedNow;
                SendCount = 0;
            }

            SendCount++;
            Generation = generation;
            CodeHash = codeHash.Trim();
            ExpiresAtUtc = NormalizeUtc(expiresAtUtc);
            ResendAvailableAtUtc = NormalizeUtc(resendAvailableAtUtc);
            FailedAttemptCount = 0;
            LockedUntilUtc = null;
            ConsumedAtUtc = null;
            RotateConcurrencyStamp();
        }

        /// <summary>
        /// Re-sends the code of the current cycle. The generation is retained so
        /// the code the owner already received stays the valid one.
        /// </summary>
        public void Resend(
            string codeHash,
            Guid generation,
            DateTime utcNow,
            DateTime expiresAtUtc,
            DateTime resendAvailableAtUtc,
            int maxSends,
            TimeSpan sendWindow)
        {
            var normalizedNow = NormalizeUtc(utcNow);

            if (!CanSendAt(normalizedNow, maxSends, sendWindow))
            {
                throw new DomainException("A library password reset code cannot be sent yet.");
            }

            if (!IsCurrentGeneration(generation))
            {
                throw new DomainException(
                    "A resend must retain the current password reset generation.");
            }

            ValidateIssue(codeHash, generation, normalizedNow, expiresAtUtc, resendAvailableAtUtc);

            if (!SendWindowStartedAtUtc.HasValue ||
                normalizedNow >= SendWindowStartedAtUtc.Value.Add(sendWindow))
            {
                SendWindowStartedAtUtc = normalizedNow;
                SendCount = 0;
            }

            SendCount++;
            CodeHash = codeHash.Trim();
            ExpiresAtUtc = NormalizeUtc(expiresAtUtc);
            ResendAvailableAtUtc = NormalizeUtc(resendAvailableAtUtc);
            RotateConcurrencyStamp();
        }

        public bool RecordFailedAttempt(
            DateTime utcNow,
            int maxFailedAttempts,
            TimeSpan lockoutPeriod)
        {
            if (maxFailedAttempts <= 0)
            {
                throw new DomainException("Maximum failed attempts must be greater than zero.");
            }

            if (lockoutPeriod <= TimeSpan.Zero)
            {
                throw new DomainException("Verification lockout period must be greater than zero.");
            }

            var normalizedNow = NormalizeUtc(utcNow);
            if (!CanVerifyAt(normalizedNow))
            {
                throw new DomainException("The library password reset challenge is not active.");
            }

            FailedAttemptCount++;
            var isLockedOut = FailedAttemptCount >= maxFailedAttempts;
            if (isLockedOut)
            {
                LockedUntilUtc = normalizedNow.Add(lockoutPeriod);
                CodeHash = null;
                ExpiresAtUtc = normalizedNow;

                if (ResendAvailableAtUtc < LockedUntilUtc.Value)
                {
                    ResendAvailableAtUtc = LockedUntilUtc.Value;
                }
            }

            RotateConcurrencyStamp();
            return isLockedOut;
        }

        /// <summary>Burns the code once the new password has been set.</summary>
        public void MarkConsumed(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            if (!CanVerifyAt(normalizedNow))
            {
                throw new DomainException("The library password reset challenge is not active.");
            }

            ConsumedAtUtc = normalizedNow;
            CodeHash = null;
            RotateConcurrencyStamp();
        }

        public bool TryCompensateDefiniteNotSent(Guid generation, Guid deliveryAttemptStamp)
        {
            if (!IsCurrentGeneration(generation)
                || deliveryAttemptStamp == Guid.Empty
                || ConcurrencyStamp != deliveryAttemptStamp
                || ConsumedAtUtc.HasValue
                || SendCount <= 0)
            {
                return false;
            }

            SendCount--;
            RotateConcurrencyStamp();
            return true;
        }

        /// <summary>Whether the current cycle may be re-sent.</summary>
        public bool CanSendAt(DateTime utcNow, int maxSends, TimeSpan sendWindow)
        {
            var normalizedNow = NormalizeUtc(utcNow);

            return !ConsumedAtUtc.HasValue
                && CanStartCycleAt(normalizedNow, maxSends, sendWindow);
        }

        /// <summary>
        /// Whether a brand-new cycle may begin. Unlike <see cref="CanSendAt"/>
        /// a consumed challenge qualifies: the previous reset finished, and the
        /// owner is entitled to reset again.
        /// </summary>
        public bool CanStartCycleAt(DateTime utcNow, int maxSends, TimeSpan sendWindow)
        {
            if (maxSends <= 0 || sendWindow <= TimeSpan.Zero)
            {
                return false;
            }

            var normalizedNow = NormalizeUtc(utcNow);

            if (IsLockedAt(normalizedNow) || normalizedNow < ResendAvailableAtUtc)
            {
                return false;
            }

            return !SendWindowStartedAtUtc.HasValue
                || normalizedNow >= SendWindowStartedAtUtc.Value.Add(sendWindow)
                || SendCount < maxSends;
        }

        public bool CanVerifyAt(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            return CodeHash is not null
                && !ConsumedAtUtc.HasValue
                && !IsLockedAt(normalizedNow)
                && ExpiresAtUtc > normalizedNow;
        }

        public bool IsLockedAt(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            return LockedUntilUtc.HasValue && LockedUntilUtc.Value > normalizedNow;
        }

        public bool IsCurrentGeneration(Guid generation) =>
            generation != Guid.Empty && Generation == generation;

        private static void ValidateIssue(
            string codeHash,
            Guid generation,
            DateTime utcNow,
            DateTime expiresAtUtc,
            DateTime resendAvailableAtUtc)
        {
            if (string.IsNullOrWhiteSpace(codeHash))
            {
                throw new DomainException("Library password reset code hash is required.");
            }

            if (generation == Guid.Empty)
            {
                throw new DomainException("Library password reset generation is required.");
            }

            var normalizedExpiry = NormalizeUtc(expiresAtUtc);
            var normalizedResendAvailableAt = NormalizeUtc(resendAvailableAtUtc);

            if (normalizedExpiry <= utcNow)
            {
                throw new DomainException("Library password reset expiry must be in the future.");
            }

            if (normalizedResendAvailableAt < utcNow ||
                normalizedResendAvailableAt > normalizedExpiry)
            {
                throw new DomainException("Library password reset resend time is invalid.");
            }
        }

        private void RotateConcurrencyStamp()
        {
            ConcurrencyStamp = Guid.NewGuid();
            UpdateModificationTime();
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

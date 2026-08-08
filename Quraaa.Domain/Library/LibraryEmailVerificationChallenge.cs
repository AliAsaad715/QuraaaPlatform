using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Library
{
    public sealed class LibraryEmailVerificationChallenge : AggregateRoot
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

        private LibraryEmailVerificationChallenge() { }

        public LibraryEmailVerificationChallenge(
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
                throw new DomainException("Library email verification challenge id is required.");
            }

            if (libraryId == Guid.Empty)
            {
                throw new DomainException("Library id is required for email verification.");
            }

            var normalizedIssuedAt = NormalizeUtc(issuedAtUtc);
            ValidateIssue(
                codeHash,
                generation,
                normalizedIssuedAt,
                expiresAtUtc,
                resendAvailableAtUtc);

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

        public void Issue(
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
                throw new DomainException("A library email verification code cannot be sent yet.");
            }

            if (!IsCurrentGeneration(generation))
            {
                throw new DomainException(
                    "A resend must retain the current library email verification generation.");
            }

            ValidateIssue(
                codeHash,
                generation,
                normalizedNow,
                expiresAtUtc,
                resendAvailableAtUtc);

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

        public bool TryCompensateDefiniteNotSent(
            Guid generation,
            Guid deliveryAttemptStamp)
        {
            if (!IsCurrentGeneration(generation)
                || deliveryAttemptStamp == Guid.Empty
                || ConcurrencyStamp != deliveryAttemptStamp
                || ConsumedAtUtc.HasValue
                || SendCount <= 0)
            {
                return false;
            }

            // Keep the resend cooldown and current code lifetime intact. The caller
            // gets one quota slot back, while the cooldown and temporary session
            // still bound retries against an unavailable or rejecting SMTP server.
            SendCount--;
            RotateConcurrencyStamp();
            return true;
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
                throw new DomainException("The library email verification challenge is not active.");
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

        public void MarkConsumed(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            if (!CanVerifyAt(normalizedNow))
            {
                throw new DomainException("The library email verification challenge is not active.");
            }

            ConsumedAtUtc = normalizedNow;
            CodeHash = null;
            RotateConcurrencyStamp();
        }

        public bool CanSendAt(DateTime utcNow, int maxSends, TimeSpan sendWindow)
        {
            if (maxSends <= 0 || sendWindow <= TimeSpan.Zero)
            {
                return false;
            }

            var normalizedNow = NormalizeUtc(utcNow);
            if (ConsumedAtUtc.HasValue ||
                IsLockedAt(normalizedNow) ||
                normalizedNow < ResendAvailableAtUtc)
            {
                return false;
            }

            return !SendWindowStartedAtUtc.HasValue ||
                normalizedNow >= SendWindowStartedAtUtc.Value.Add(sendWindow) ||
                SendCount < maxSends;
        }

        public bool CanVerifyAt(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            return CodeHash is not null &&
                !ConsumedAtUtc.HasValue &&
                !IsLockedAt(normalizedNow) &&
                ExpiresAtUtc > normalizedNow;
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
                throw new DomainException("Library email verification code hash is required.");
            }

            if (generation == Guid.Empty)
            {
                throw new DomainException("Library email verification generation is required.");
            }

            var normalizedExpiry = NormalizeUtc(expiresAtUtc);
            var normalizedResendAvailableAt = NormalizeUtc(resendAvailableAtUtc);

            if (normalizedExpiry <= utcNow)
            {
                throw new DomainException("Library email verification expiry must be in the future.");
            }

            if (normalizedResendAvailableAt < utcNow ||
                normalizedResendAvailableAt > normalizedExpiry)
            {
                throw new DomainException("Library email verification resend time is invalid.");
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

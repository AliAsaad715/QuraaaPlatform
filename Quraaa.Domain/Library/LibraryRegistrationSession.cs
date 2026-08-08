using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Library
{
    public sealed class LibraryRegistrationSession : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid AuthenticationSessionId { get; private set; }
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAtUtc { get; private set; }
        public DateTime? SubmittedAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public Guid ConcurrencyStamp { get; private set; }

        private LibraryRegistrationSession() { }

        public LibraryRegistrationSession(
            Guid id,
            Guid userId,
            Guid authenticationSessionId,
            string tokenHash,
            DateTime expiresAtUtc,
            DateTime? submittedAtUtc)
        {
            ValidateIdentity(id, userId, authenticationSessionId);
            ValidateTokenHash(tokenHash);

            var normalizedExpiry = NormalizeUtc(expiresAtUtc);
            DateTime? normalizedSubmittedAt = submittedAtUtc.HasValue
                ? NormalizeUtc(submittedAtUtc.Value)
                : null;

            ValidateExpiry(normalizedExpiry, normalizedSubmittedAt);

            Id = id;
            UserId = userId;
            AuthenticationSessionId = authenticationSessionId;
            TokenHash = tokenHash.Trim();
            ExpiresAtUtc = normalizedExpiry;
            SubmittedAtUtc = normalizedSubmittedAt;
            ConcurrencyStamp = Guid.NewGuid();
        }

        public void Reissue(
            Guid authenticationSessionId,
            string tokenHash,
            DateTime expiresAtUtc,
            DateTime? submittedAtUtc)
        {
            if (CompletedAtUtc.HasValue)
            {
                throw new DomainException("A completed library registration session cannot be reissued.");
            }

            if (authenticationSessionId == Guid.Empty)
            {
                throw new DomainException("Authentication session id is required.");
            }

            ValidateTokenHash(tokenHash);

            var normalizedExpiry = NormalizeUtc(expiresAtUtc);
            DateTime? normalizedSubmittedAt = submittedAtUtc.HasValue
                ? NormalizeUtc(submittedAtUtc.Value)
                : SubmittedAtUtc;

            ValidateExpiry(normalizedExpiry, normalizedSubmittedAt);

            AuthenticationSessionId = authenticationSessionId;
            TokenHash = tokenHash.Trim();
            ExpiresAtUtc = normalizedExpiry;
            SubmittedAtUtc ??= normalizedSubmittedAt;
            RotateConcurrencyStamp();
        }

        public void MarkSubmitted(DateTime utcNow, DateTime expiresAtUtc)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            var normalizedExpiry = NormalizeUtc(expiresAtUtc);

            if (!IsActiveAt(normalizedNow))
            {
                throw new DomainException("The library registration session is no longer active.");
            }

            if (SubmittedAtUtc.HasValue)
            {
                throw new DomainException("Library registration details have already been submitted.");
            }

            if (normalizedExpiry <= normalizedNow)
            {
                throw new DomainException("The submitted registration session expiry must be in the future.");
            }

            SubmittedAtUtc = normalizedNow;
            ExpiresAtUtc = normalizedExpiry;
            RotateConcurrencyStamp();
        }

        public void Complete(DateTime utcNow)
        {
            if (CompletedAtUtc.HasValue)
            {
                return;
            }

            var normalizedNow = NormalizeUtc(utcNow);
            if (!IsActiveAt(normalizedNow))
            {
                throw new DomainException("The library registration session is no longer active.");
            }

            if (!SubmittedAtUtc.HasValue)
            {
                throw new DomainException("Library registration details must be submitted before completion.");
            }

            CompletedAtUtc = normalizedNow;
            RotateConcurrencyStamp();
        }

        public bool IsActiveAt(DateTime utcNow)
        {
            var normalizedNow = NormalizeUtc(utcNow);
            return !CompletedAtUtc.HasValue && ExpiresAtUtc > normalizedNow;
        }

        private static void ValidateIdentity(
            Guid id,
            Guid userId,
            Guid authenticationSessionId)
        {
            if (id == Guid.Empty)
            {
                throw new DomainException("Library registration session id is required.");
            }

            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a library registration session.");
            }

            if (authenticationSessionId == Guid.Empty)
            {
                throw new DomainException("Authentication session id is required.");
            }
        }

        private static void ValidateTokenHash(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                throw new DomainException("Library registration token hash is required.");
            }
        }

        private static void ValidateExpiry(
            DateTime expiresAtUtc,
            DateTime? submittedAtUtc)
        {
            if (expiresAtUtc <= DateTime.UtcNow)
            {
                throw new DomainException("Library registration session expiry must be in the future.");
            }

            if (submittedAtUtc.HasValue && submittedAtUtc.Value >= expiresAtUtc)
            {
                throw new DomainException("Library registration submission time must precede session expiry.");
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

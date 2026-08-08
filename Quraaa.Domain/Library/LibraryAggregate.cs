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
        public Guid ConcurrencyStamp { get; private set; }

        private LibraryAggregate() { }

        public LibraryAggregate(
            Guid id,
            string libraryName,
            string location,
            string libraryImage,
            string headerImage,
            string email,
            Guid userId)
        {
            Id = id;
            LibraryName = libraryName;
            Location = location;
            LibraryImage = libraryImage;
            HeaderImage = headerImage;
            Email = email;
            UserId = userId;
            ApprovalStatus = LibraryApprovalStatus.AwaitingEmailVerification;
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

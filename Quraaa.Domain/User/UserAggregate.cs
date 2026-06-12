using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Enums;
using Quraaa.Domain.User.ValueObjects;

namespace Quraaa.Domain.User
{
    public class UserAggregate : AggregateRoot
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string PhoneNumber { get; private set; }
        public string PasswordHash { get; private set; }
        public Gender Gender { get; private set; }
        public Role Role { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string? ProfileImageUrl { get; private set; }
        public DateTime? LastLoginDate { get; private set; }
        public DateTime? PreviousLoginDate { get; private set; }
        public PaymentMethodInfo? PaymentMethod { private set; get; }


        private readonly List<string> _interests = new();
        public IReadOnlyCollection<string> Interests => _interests.AsReadOnly();

        private UserAggregate() { }

        public UserAggregate(Guid id, string firstName, string lastName, string phoneNumber, string passwordHash, Gender gender, Role role, DateOnly dateOfBirth)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            PasswordHash = passwordHash;
            Gender = gender;
            Role = role;
            DateOfBirth = dateOfBirth;
        }

        public void LinkPaymentMethod(string customerId, string brand, string lastFour)
        {
            PaymentMethod = new PaymentMethodInfo(customerId, brand, lastFour);
        }

        public void AddInterest(string interestCode)
        {
            var verifiedInterest = Interest.FromCode(interestCode);

            if (verifiedInterest == null)
            {
                throw new DomainException($"The interest code '{interestCode}' is invalid and not registered in the domain constants.");
            }

            if (!_interests.Contains(verifiedInterest.Code))
            {
                _interests.Add(verifiedInterest.Code);
            }
        }

        public void UpdatePasswordHash(string passwordHash, Guid modifiedBy)
        {
            PasswordHash = passwordHash;
            UpdateAudit(modifiedBy);
        }
    }
}

using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.User.Entities;
using Quraaa.Domain.User.Enums;
using Quraaa.Domain.User.ValueObjects;

namespace Quraaa.Domain.User
{
    public class UserAggregate : AggregateRoot
    {
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string PhoneNumber { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public Gender Gender { get; private set; }
        public Role Role { get; private set; }
        public DateOnly DateOfBirth { get; private set; }
        public string? ProfileImageUrl { get; private set; }
        public DateTime? LastLoginDate { get; private set; }
        public DateTime? PreviousLoginDate { get; private set; }
        public PaymentMethodInfo? PaymentMethod { private set; get; }

        private readonly List<Interest> _interests = new();
        public IReadOnlyCollection<Interest> Interests => _interests.AsReadOnly();
        public IReadOnlyCollection<Guid> InterestedCategoryIds =>
            _interests.Select(i => i.CategoryId).ToList().AsReadOnly();

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

        public void AddInterest(Guid categoryId)
        {
            if (_interests.Any(i => i.CategoryId == categoryId))
            {
                return;
            }

            _interests.Add(new Interest(Id, categoryId));
        }

        public void RemoveInterest(Guid categoryId)
        {
            _interests.RemoveAll(i => i.CategoryId == categoryId);
        }

        public void UpdatePasswordHash(string passwordHash, Guid modifiedBy)
        {
            PasswordHash = passwordHash;
            UpdateAudit(modifiedBy);
        }

        public void UpdateProfile(
            string firstName,
            string lastName,
            Gender gender,
            DateOnly dateOfBirth,
            string? profileImageUrl,
            IEnumerable<Guid> categoryIds,
            Guid modifiedBy)
        {
            FirstName = firstName;
            LastName = lastName;
            Gender = gender;
            DateOfBirth = dateOfBirth;
            ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl;

            _interests.Clear();
            _interests.AddRange(categoryIds.Distinct().Select(id => new Interest(Id, id)));

            UpdateAudit(modifiedBy);
        }
    }
}

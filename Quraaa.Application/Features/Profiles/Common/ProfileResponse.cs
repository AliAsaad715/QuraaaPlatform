using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Profiles.Common
{
    public record ProfileResponse(
        Guid UserId,
        string FirstName,
        string LastName,
        string PhoneNumber,
        Gender Gender,
        Role Role,
        DateOnly DateOfBirth,
        string? ProfileImageUrl,
        IReadOnlyCollection<string> Interests,
        DateTime? LastLoginDate,
        DateTime? PreviousLoginDate,
        DateTime CreationTime,
        DateTime? LastModificationTime
    )
    {
        public static ProfileResponse FromUser(UserAggregate user)
        {
            return new ProfileResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Gender,
                user.Role,
                user.DateOfBirth,
                user.ProfileImageUrl,
                user.Interests.ToList(),
                user.LastLoginDate,
                user.PreviousLoginDate,
                user.CreationTime,
                user.LastModificationTime
            );
        }
    }
}

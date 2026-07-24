using Quraaa.Application.Features.Categories.Common;
using Quraaa.Domain.Category;
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
        IReadOnlyCollection<CategoryResponse> Interests,
        LocationResponse? Location,
        DateTime? LastLoginDate,
        DateTime? PreviousLoginDate,
        DateTime CreationTime,
        DateTime? LastModificationTime
    )
    {
        public static ProfileResponse FromUser(UserAggregate user, IReadOnlyCollection<CategoryAggregate> interestCategories)
        {
            var categoriesById = interestCategories.ToDictionary(category => category.Id);
            var interests = new List<CategoryResponse>();

            foreach (var categoryId in user.InterestedCategoryIds)
            {
                if (categoriesById.TryGetValue(categoryId, out var category))
                {
                    interests.Add(new CategoryResponse(category.Id, category.NameAr, category.NameEn));
                }
            }

            return new ProfileResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Gender,
                user.Role,
                user.DateOfBirth,
                user.ProfileImageUrl,
                interests,
                user.Location != null ? new LocationResponse(user.Location.Latitude, user.Location.Longitude) : null,
                user.LastLoginDate,
                user.PreviousLoginDate,
                user.CreationTime,
                user.LastModificationTime
            );
        }
    }
}

public record LocationResponse(
    double Latitude,
    double Longitude
);
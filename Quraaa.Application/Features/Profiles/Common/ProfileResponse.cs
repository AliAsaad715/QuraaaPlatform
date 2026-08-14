using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Category;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Entities;
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
        IReadOnlyCollection<LocationResponse> Locations,
        DateTime? LastLoginDate,
        DateTime? PreviousLoginDate,
        DateTime CreationTime,
        DateTime? LastModificationTime
    )
    {
        public static ProfileResponse FromUser(
            UserAggregate user,
            IReadOnlyCollection<CategoryAggregate> interestCategories,
            IImageUrlFormatter imageUrlFormatter)
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

            var locations = user.Locations
                .OrderByDescending(location => location.Id == user.DefaultLocationId)
                .ThenBy(location => location.CreationTime)
                .ThenBy(location => location.Id)
                .Select(location => LocationResponse.FromLocation(
                    location,
                    user.DefaultLocationId))
                .ToArray();

            var defaultLocation = user.DefaultLocation is { } savedDefault
                ? LocationResponse.FromLocation(savedDefault, user.DefaultLocationId)
                : null;

            return new ProfileResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Gender,
                user.Role,
                user.DateOfBirth,
                imageUrlFormatter.Format(user.ProfileImageUrl),
                interests,
                defaultLocation,
                locations,
                user.LastLoginDate,
                user.PreviousLoginDate,
                user.CreationTime,
                user.LastModificationTime
            );
        }
    }

    public record LocationResponse(
        Guid Id,
        string Name,
        string? Address,
        double Latitude,
        double Longitude,
        bool IsDefault,
        DateTime CreationTime,
        DateTime? LastModificationTime)
    {
        public static LocationResponse FromLocation(
            UserLocation location,
            Guid? defaultLocationId) =>
            new(
                location.Id,
                location.Name,
                location.Address,
                location.Latitude,
                location.Longitude,
                location.Id == defaultLocationId,
                location.CreationTime,
                location.LastModificationTime);
    }
}

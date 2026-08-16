using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.User;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class DemoProfileSeeder
{
    private sealed record LocationSeed(
        string Name,
        string Address,
        double Latitude,
        double Longitude,
        bool IsDefault = false);

    private sealed record ProfileSeed(
        string PhoneNumber,
        IReadOnlyCollection<Guid> CategoryIds,
        IReadOnlyCollection<LocationSeed> Locations,
        bool HasPaymentMethod = false);

    public static async Task SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var definitions = GetDefinitions();
        var phoneNumbers = definitions.Select(definition => definition.PhoneNumber).ToArray();

        var profiles = await context.UsersProfiles
            .Include(profile => profile.Interests)
            .Include(profile => profile.Locations)
            .Where(profile => phoneNumbers.Contains(profile.PhoneNumber))
            .ToListAsync(cancellationToken);

        var profileByPhone = profiles.ToDictionary(
            profile => profile.PhoneNumber,
            StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            if (!profileByPhone.TryGetValue(definition.PhoneNumber, out var profile))
            {
                continue;
            }

            foreach (var categoryId in definition.CategoryIds)
            {
                profile.AddInterest(categoryId);
            }

            foreach (var locationDefinition in definition.Locations)
            {
                var existingLocation = profile.Locations.FirstOrDefault(location =>
                    string.Equals(
                        location.Name,
                        locationDefinition.Name,
                        StringComparison.OrdinalIgnoreCase));

                if (existingLocation is null)
                {
                    profile.AddLocation(
                        locationDefinition.Name,
                        locationDefinition.Address,
                        locationDefinition.Latitude,
                        locationDefinition.Longitude,
                        locationDefinition.IsDefault,
                        profile.Id);
                }
                else if (locationDefinition.IsDefault &&
                         profile.DefaultLocationId != existingLocation.Id)
                {
                    profile.SetDefaultLocation(existingLocation.Id, profile.Id);
                }
            }

            if (definition.HasPaymentMethod && profile.PaymentMethod is null)
            {
                profile.LinkPaymentMethod(
                    "cus_demo_main_buyer",
                    "Visa",
                    "4242");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<ProfileSeed> GetDefinitions() =>
    [
        new(
            DemoSeedData.MainBuyerPhoneNumber,
            [CategoryIds.Novels, CategoryIds.Literature, CategoryIds.Technology],
            [
                new("Home", "Al-Karrada, Baghdad", 33.2981, 44.4089, IsDefault: true),
                new("Work", "Al-Mansour, Baghdad", 33.3128, 44.3366),
                new("University", "University of Baghdad, Jadriya", 33.2734, 44.3770),
            ],
            HasPaymentMethod: true),
        new(
            DemoSeedData.SellerPhoneNumber,
            [CategoryIds.Literature, CategoryIds.Art, CategoryIds.History],
            [new("Home", "Al-Adhamiya, Baghdad", 33.3708, 44.3720, IsDefault: true)]),
        new(
            DemoSeedData.CheckoutBuyerPhoneNumber,
            [CategoryIds.Technology, CategoryIds.Science, CategoryIds.Education],
            [new("Home", "Al-Jadriya, Baghdad", 33.2768, 44.3836, IsDefault: true)]),
        new(
            DemoSeedData.ReporterOnePhoneNumber,
            [CategoryIds.Novels, CategoryIds.Culture],
            [new("Home", "Al-Harithiya, Baghdad", 33.3224, 44.3505, IsDefault: true)]),
        new(
            DemoSeedData.ReporterTwoPhoneNumber,
            [CategoryIds.History, CategoryIds.Geography],
            [new("Home", "Al-Ashar, Basra", 30.5085, 47.7804, IsDefault: true)]),
        new(
            DemoSeedData.ReporterThreePhoneNumber,
            [CategoryIds.Science, CategoryIds.SpaceScience],
            [new("Home", "Ankawa, Erbil", 36.2285, 43.9930, IsDefault: true)]),
    ];
}

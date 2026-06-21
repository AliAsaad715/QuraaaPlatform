using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Library;
using Quraaa.Domain.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quraaa.Persistence.Seed
{
    public static class LibrarySeeder
    {
        public static async Task SeedAsync(DbContext db)
        {
            var librarySet = db.Set<LibraryAggregate>();

            if (await librarySet.AnyAsync())
            {
                return;
            }

            // Fetch the deterministic users created specifically to own seeded libraries.
            var ownerPhoneNumbers = UserSeeder.LibraryOwnerPhoneNumbers.ToArray();
            var userIds = await db.Set<UserAggregate>()
                .Where(u => ownerPhoneNumbers.Contains(u.PhoneNumber))
                .OrderBy(u => u.PhoneNumber)
                .Select(u => u.Id)
                .ToListAsync();

            if (!userIds.Any())
            {
                return;
            }

            var librarySeedCount = Math.Min(UserSeeder.LibraryOwnerCount, userIds.Count);
            var libraries = new List<LibraryAggregate>();

            string[] names = { "Dar Al-Hikma", "Al-Qalam", "Beacon of Light", "Al-Ma'rifa", "BookHaven", "The Reading Room", "Horizon Knowledge", "Wisdom Hub", "Al-Amal", "Enlighten" };
            string[] suffixes = { "Central Library", "Cultural Center", "Community Books", "Reading Lounge", "Knowledge Oasis" };
            string[] zones = { "Downtown", "Academic District", "Al-Rawda", "Northern Zone", "Al-Hamra", "West End", "Innovation Hub" };

            int count = 0;
            for (int i = 0; i < names.Length; i++)
            {
                for (int j = 0; j < suffixes.Length; j++)
                {
                    for (int k = 0; k < zones.Length; k++)
                    {
                        if (count >= librarySeedCount) break;

                        var libraryNumber = count + 1;
                        var id = Guid.NewGuid();
                        var libraryName = $"{names[i]} {suffixes[j]} ({zones[k]} Branch)";
                        var location = $"{zones[k]} Street, Block {libraryNumber % 10 + 1}";
                        var libraryImage = "https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=500&q=80";
                        var headerImage = "https://images.unsplash.com/photo-1521587760476-6c12a4b040da?w=1200&q=80";
                        var email = $"info.lib{libraryNumber}@quraaa.com";

                        var userId = userIds[count];

                        var library = new LibraryAggregate(
                            id,
                            libraryName,
                            location,
                            libraryImage,
                            headerImage,
                            email,
                            userId
                        );

                        if (libraryNumber % 4 != 0)
                        {
                            library.Approve(Guid.NewGuid());
                        }

                        libraries.Add(library);
                        count++;
                    }
                    if (count >= librarySeedCount) break;
                }
                if (count >= librarySeedCount) break;
            }

            await librarySet.AddRangeAsync(libraries);
            await db.SaveChangesAsync();
        }
    }
}

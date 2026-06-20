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

            // 1. Fetch existing real user IDs from the database profile table
            var userIds = await db.Set<UserAggregate>().Select(u => u.Id).ToListAsync();

            if (!userIds.Any())
            {
                return;
            }

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
                        if (count >= 100) break;

                        count++;
                        var id = Guid.NewGuid();
                        var libraryName = $"{names[i]} {suffixes[j]} ({zones[k]} Branch)";
                        var location = $"{zones[k]} Street, Block {count % 10 + 1}";
                        var libraryImage = "https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=500&q=80";
                        var headerImage = "https://images.unsplash.com/photo-1521587760476-6c12a4b040da?w=1200&q=80";
                        var email = $"info.lib{count}@quraaa.com";

                        // 2. Map library to an existing user rotationally
                        var userId = userIds[count % userIds.Count];

                        var library = new LibraryAggregate(
                            id,
                            libraryName,
                            location,
                            libraryImage,
                            headerImage,
                            email,
                            userId
                        );

                        if (count % 4 != 0)
                        {
                            library.Approve(Guid.NewGuid());
                        }

                        libraries.Add(library);
                    }
                    if (count >= 100) break;
                }
                if (count >= 100) break;
            }

            await librarySet.AddRangeAsync(libraries);
            await db.SaveChangesAsync();
        }
    }
}
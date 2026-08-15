using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quraaa.Application.Features.Listings.Interfaces;

namespace Quraaa.API.IntegrationTests
{
    /// <summary>
    /// Boots the real API pipeline (routing, MediatR, FluentValidation, AppResult mapping)
    /// without a real Postgres instance: Program.cs skips migration/seeding in the
    /// "Testing" environment, and the real Google Books client is replaced by a fake.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeBookMetadataService BookMetadataService { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                // Migration/seeding is skipped in "Testing", so this connection string is
                // never actually connected to — it only needs to be syntactically valid so
                // AddDbContext/UseNpgsql doesn't fail to configure at startup.
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=quraaa_test_unused;Username=test;Password=test"
                });
            });

            builder.ConfigureServices(services =>
            {
                // These endpoint tests don't need background workers, and several of them
                // poll the database on their own schedule — never start them here.
                services.RemoveAll<IHostedService>();

                services.RemoveAll<IBookMetadataService>();
                services.AddSingleton<IBookMetadataService>(BookMetadataService);
            });
        }
    }
}

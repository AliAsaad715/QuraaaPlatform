using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Quraaa.Persistence.Data;

namespace Quraaa.API.DesignTime
{
    public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var apiProjectDirectory = ResolveApiProjectDirectory();
            LoadEnvFile(apiProjectDirectory);

            var environmentName =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private static string ResolveApiProjectDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
            {
                return currentDirectory;
            }

            var directory = new DirectoryInfo(currentDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Quraaa.API");

                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return currentDirectory;
        }

        private static void LoadEnvFile(string apiProjectDirectory)
        {
            var candidates = new[]
            {
                Path.Combine(apiProjectDirectory, ".env"),
                Path.GetFullPath(Path.Combine(apiProjectDirectory, "..", ".env")),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };

            foreach (var envPath in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(envPath))
                {
                    DotNetEnv.Env.Load(envPath);
                    return;
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Interceptors;

namespace Quraaa.API.Extensions
{
    public static class DatabaseExtensions
    {
        public static void AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(connectionString)
                    .AddInterceptors(serviceProvider.GetRequiredService<DomainEventOutboxInterceptor>()));

            // Register Identity services with custom options
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.Password.RequiredLength = AuthenticationPasswordPolicy.MinimumLength;
                options.Password.RequiredUniqueChars = AuthenticationPasswordPolicy.RequiredUniqueCharacters;
                options.Password.RequireDigit = AuthenticationPasswordPolicy.RequireDigit;
                options.Password.RequireLowercase = AuthenticationPasswordPolicy.RequireLowercase;
                options.Password.RequireUppercase = AuthenticationPasswordPolicy.RequireUppercase;
                options.Password.RequireNonAlphanumeric = AuthenticationPasswordPolicy.RequireNonAlphanumeric;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        }
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quraaa.API.Services;
using Quraaa.Application.Extensions;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Persistence.Extensions;
using System.Security.Claims;
using System.Text;

namespace Quraaa.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddJwtAuthentication(configuration);
            services.AddScoped<ILibraryImageStorageService, LibraryImageStorageService>();
            services.AddHostedService<ExpiredOrderPaymentReconciliationService>();
            PersistenceDependencyInjectionHandler.AddPersistenceDependencies(services, configuration);
            ApplicationPackagesRegisterExtensions.AddApplicationDependencies(services);
        }

        private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration["JWT_SECRET_KEY"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is missing.");
            }

            var issuer = configuration["JWT_ISSUER"];
            var audience = configuration["JWT_AUDIENCE"];

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                        ValidIssuer = issuer,
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            services.AddAuthorization();
        }
    }
}

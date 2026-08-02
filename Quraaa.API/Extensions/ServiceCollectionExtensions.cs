using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quraaa.API.Services;
using Quraaa.Application.Extensions;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Persistence.Extensions;
using System.IdentityModel.Tokens.Jwt;
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
            services.AddScoped<ILibraryBookStorageService, LibraryBookStorageService>();
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

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var tokenId = context.Principal?
                                .FindFirstValue(JwtRegisteredClaimNames.Jti);

                            if (string.IsNullOrWhiteSpace(tokenId))
                            {
                                context.Fail("Access token does not contain a token identifier.");
                                return;
                            }

                            var revocationService = context.HttpContext.RequestServices
                                .GetRequiredService<IAccessTokenRevocationService>();

                            if (await revocationService.IsRevokedAsync(
                                    tokenId,
                                    context.HttpContext.RequestAborted))
                            {
                                context.Fail("Access token has been revoked.");
                                return;
                            }

                            var userIdValue = context.Principal?
                                .FindFirstValue(ClaimTypes.NameIdentifier);
                            var familyIdValue = context.Principal?
                                .FindFirstValue(AuthenticationClaimNames.SessionId)
                                ?? context.Principal?.FindFirstValue(ClaimTypes.Sid);

                            if (!Guid.TryParse(userIdValue, out var userId)
                                || !Guid.TryParse(familyIdValue, out var familyId))
                            {
                                context.Fail("Access token does not contain a valid session identifier.");
                                return;
                            }

                            var identityService = context.HttpContext.RequestServices
                                .GetRequiredService<IIdentityService>();

                            if (!await identityService.IsRefreshTokenFamilyActiveAsync(
                                    userId,
                                    familyId,
                                    context.HttpContext.RequestAborted))
                            {
                                context.Fail("Access-token session has been revoked or replaced.");
                            }
                        }
                    };
                });

            services.AddAuthorization();
        }
    }
}

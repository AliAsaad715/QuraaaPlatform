using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Quraaa.API.Services;
using Quraaa.Application.Extensions;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Files;
using Quraaa.Persistence.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace Quraaa.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddJwtAuthentication(configuration);
            services.AddAuthenticationRateLimiting();
            services.Configure<FileStorageOptions>(configuration.GetSection("Storage"));
            services.Configure<FileRetentionOptions>(configuration.GetSection("Storage:FileRetention"));
            services.AddScoped<IFileStorageService, PrivateFileStorageService>();
            services.AddScoped<IFileAccessService, FileAccessService>();
            services.AddScoped<ILibraryImageStorageService, LibraryImageStorageService>();
            services.AddScoped<ILibraryBookStorageService, LibraryBookStorageService>();
            services.AddScoped<IBulkBookStorageService, BulkBookStorageService>();
            services.AddHostedService<ExpiredOrderPaymentReconciliationService>();
            services.AddHostedService<FileRetentionCleanupService>();
            PersistenceDependencyInjectionHandler.AddPersistenceDependencies(services, configuration);
            ApplicationPackagesRegisterExtensions.AddApplicationDependencies(services);
        }

        private static void AddAuthenticationRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("regular-login", httpContext =>
                {
                    var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown-client";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });
            });
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
            var durationValue = configuration["JWT_DURATION_IN_MINUTES"] ?? "60";
            if (!double.TryParse(
                    durationValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var durationInMinutes)
                || !double.IsFinite(durationInMinutes)
                || durationInMinutes <= 0
                || durationInMinutes > TimeSpan.FromDays(7).TotalMinutes)
            {
                throw new InvalidOperationException(
                    "JWT_DURATION_IN_MINUTES must be an invariant positive number no greater than 10080.");
            }

            services.AddSingleton(new AuthenticationTokenOptions
            {
                AccessTokenDurationInMinutes = durationInMinutes
            });

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

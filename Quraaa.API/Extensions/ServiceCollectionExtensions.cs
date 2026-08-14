using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Quraaa.API.Services;
using Quraaa.Application.Extensions;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
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
        public const string LibraryDashboardCorsPolicy = "library-dashboard";
        public const string LibraryRegistrationLinkRateLimitPolicy = "library-registration-link";
        public const string LibraryRegistrationPublicRateLimitPolicy = "library-registration-public";
        public const string LibraryWalletRateLimitPolicy = "library-wallet";

        public static void AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            var libraryRegistrationOptions = CreateLibraryRegistrationOptions(
                configuration,
                isDevelopment);

            services.AddJwtAuthentication(configuration);
            services.AddAuthenticationRateLimiting();
            services.Configure<FileStorageOptions>(configuration.GetSection("Storage"));
            services.Configure<FileRetentionOptions>(configuration.GetSection("Storage:FileRetention"));
            services.AddScoped<IFileAccessService, FileAccessService>();
            services.AddHttpClient("PrivateAssetDelivery", client =>
            {
                // Large ebooks are streamed until the caller disconnects. RequestAborted
                // remains the timeout/cancellation authority for this proxy client.
                client.Timeout = Timeout.InfiniteTimeSpan;
            });
            services.AddLogging(logging => logging.AddFilter(
                "System.Net.Http.HttpClient.PrivateAssetDelivery",
                LogLevel.None));
            services.AddSingleton(libraryRegistrationOptions);
            services.AddCors(options =>
            {
                options.AddPolicy(LibraryDashboardCorsPolicy, policy =>
                {
                    policy
                        .WithOrigins(libraryRegistrationOptions.DashboardRegisterUrl.GetLeftPart(UriPartial.Authority))
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            services.AddScoped<ILibraryImageStorageService, LibraryImageStorageService>();
            services.AddScoped<ILibraryBookStorageService, LibraryBookStorageService>();
            services.AddScoped<IBulkBookStorageService, BulkBookStorageService>();
            services.AddHostedService<ExpiredOrderPaymentReconciliationService>();
            services.AddHostedService<FileRetentionCleanupService>();
            services.AddHostedService<SellerPayoutProcessingService>();
            services.AddOptions<PayoutOptions>()
                .Bind(configuration.GetSection("Payouts"))
                .Validate(
                    options => options.PlatformCommissionPercent is >= 0m and <= 100m,
                    "Payouts:PlatformCommissionPercent must be between 0 and 100.")
                .Validate(
                    options => decimal.Round(options.PlatformCommissionPercent, 2)
                        == options.PlatformCommissionPercent,
                    "Payouts:PlatformCommissionPercent must have at most 2 decimal places, " +
                    "matching the decimal(5,2) precision persisted on payout records.")
                .Validate(
                    options => options.MaxTransferAttempts is >= 1 and <= 100,
                    "Payouts:MaxTransferAttempts must be between 1 and 100.")
                .ValidateOnStart();
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

                options.AddPolicy(LibraryRegistrationLinkRateLimitPolicy, httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown-client";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userId ?? clientAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                // Every wallet PUT performs a live Stripe Account retrieve, so
                // keep it from becoming an account-probing / quota-burning
                // oracle.
                options.AddPolicy(LibraryWalletRateLimitPolicy, httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown-client";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userId ?? clientAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                options.AddPolicy(LibraryRegistrationPublicRateLimitPolicy, httpContext =>
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

        private static LibraryRegistrationOptions CreateLibraryRegistrationOptions(
            IConfiguration configuration,
            bool isDevelopment)
        {
            var configuredUrl = configuration["LIBRARY_DASHBOARD_REGISTER_URL"]?.Trim();
            if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var dashboardRegisterUrl)
                || (dashboardRegisterUrl.Scheme != Uri.UriSchemeHttps
                    && !(isDevelopment
                        && dashboardRegisterUrl.Scheme == Uri.UriSchemeHttp
                        && dashboardRegisterUrl.IsLoopback))
                || !string.IsNullOrEmpty(dashboardRegisterUrl.Query)
                || !string.IsNullOrEmpty(dashboardRegisterUrl.Fragment)
                || !string.IsNullOrEmpty(dashboardRegisterUrl.UserInfo))
            {
                throw new InvalidOperationException(
                    "LIBRARY_DASHBOARD_REGISTER_URL must be an absolute HTTPS URL without embedded credentials, a query string, or a fragment. Development may use HTTP only for a loopback URL.");
            }

            return new LibraryRegistrationOptions
            {
                DashboardRegisterUrl = dashboardRegisterUrl
            };
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
